using gumonji.Network;
using gumonji.Common.World;
using gumonji.Common.DAL.Repositories;

namespace gumonji.Common;

public sealed record EmuOptions
{
    public string BindAddress { get; init; } = "0.0.0.0";
    public int FemsgPort { get; init; } = 12421;
    public int ZonePort { get; init; } = 23432;
    public bool EnableZone { get; init; } = true;
    public string BackdBindAddress { get; init; } = "127.0.0.1";
    public int BackdPort { get; init; } = 12422;
    public string? BackdPassword { get; init; }
    public bool BackdOnly { get; init; }
    public string AdvertiseIp { get; init; } = "127.0.0.1";
    public int HomeZone { get; init; } = 1;
    public string? Otp { get; init; }
    public string DatabasePath { get; init; } = "gumonji.db";
    public bool Verbose { get; init; }
}

public static class SessionState
{
    public const string WaitLogin = "WAIT_LOGIN";
    public const string WaitZoneRequest = "WAIT_ZONE_REQUEST";
    public const string HandoffIssued = "HANDOFF_ISSUED";
    public const string WaitCheckPassword = "WAIT_CHECK_PASSWORD";
    public const string WaitCharacterCheck = "WAIT_CHARACTER_CHECK";
    public const string WaitCharacterLoad = "WAIT_CHARACTER_LOAD";
    public const string CharacterCreationMenu = "CHARACTER_CREATION_MENU";
    public const string CharacterCreated = "CHARACTER_CREATED";
    public const string ZoneEntered = "ZONE_ENTERED";
    public const string Disconnected = "DISCONNECTED";
}

public sealed record ActionEmoteState(uint ActionId, byte Sequence);

public sealed record ChatState(byte[] Sender, byte[] Message);

public sealed class GumonjiSession : IPacketSession
{
    private readonly Func<PacketType, byte[], CancellationToken, Task> _send;
    private long? _clockOrigin;

    public GumonjiSession(
        ServerKind kind,
        IAccountRepository accounts,
        ICharacterRepository characters,
        ILoginTokenRepository loginTokens,
        IGameplayRepository gameplay,
        EmuOptions options,
        Func<PacketType, byte[], CancellationToken, Task> send,
        ZoneRuntime? zone = null,
        Func<CancellationToken, Task>? close = null)
    {
        Kind = kind;
        Accounts = accounts;
        Characters = characters;
        LoginTokens = loginTokens;
        Gameplay = gameplay;
        Options = options;
        _send = send;
        Zone = zone ?? new ZoneRuntime();
        _close = close;
        State = kind == ServerKind.Femsg ? SessionState.WaitLogin : SessionState.WaitCheckPassword;
    }

    public ServerKind Kind { get; }
    public ZoneRuntime Zone { get; }
    private readonly Func<CancellationToken, Task>? _close;
    private readonly SemaphoreSlim _disconnect = new(1, 1);
    private bool _disconnected;
    public IAccountRepository Accounts { get; }
    public ICharacterRepository Characters { get; }
    public ILoginTokenRepository LoginTokens { get; }
    public IGameplayRepository Gameplay { get; }
    public EmuOptions Options { get; }
    public uint? UserId { get; set; }
    public uint? CharacterId { get; set; }
    public string State { get; set; }
    public bool SilentNoReply { get; set; }
    private int _sentCount;
    public int SentCount => Volatile.Read(ref _sentCount);
    public HashSet<(uint X, uint Y)> PlantedChunks { get; } = [];
    public ushort CowX
    {
        get => checked((ushort)(Zone.Find(SpawnActors.CowId)!.X / 1000));
        set => Zone.Move(SpawnActors.CowId, value * 1000u, CowY * 1000u);
    }
    public ushort CowY
    {
        get => checked((ushort)(Zone.Find(SpawnActors.CowId)!.Y / 1000));
        set => Zone.Move(SpawnActors.CowId, CowX * 1000u, value * 1000u);
    }
    public uint? EquippedVehicleId { get; set; }
    public InventoryTemplateNotice? LastInventoryTemplates { get; set; }
    public EnvironmentReportRequest? LastEnvironmentReport { get; set; }
    public uint? LastFrontendAnimationId { get; set; }
    public bool WorldVehiclePickedUp { get => Zone.VehiclePickedUp; set => Zone.VehiclePickedUp = value; }
    public ActionEmoteState? LastActionEmote { get; set; }
    public uint? LastFacialEmoteId { get; set; }
    public ChatState? LastChat { get; set; }
    public bool IsTypingInChat { get; set; }
    public uint PositionX { get; set; } = 64000;
    public uint PositionY { get; set; } = 64000;
    private long? _playStarted;
    private long _savedPlaySeconds;
    private readonly SemaphoreSlim _conditionSave = new(1, 1);

    public void StartPlayTime() => _playStarted ??= Zone.Time.GetTimestamp();

    public async Task SaveConditionAsync(uint walking = 0, uint swimming = 0, CancellationToken ct = default)
    {
        await _conditionSave.WaitAsync(ct);
        try
        {
            if (State != SessionState.ZoneEntered || UserId is null)
                throw new InvalidDataException("condition report before zone entry");
            StartPlayTime();
            var total = (long)Zone.Time.GetElapsedTime(_playStarted!.Value).TotalSeconds;
            var delta = total - _savedPlaySeconds;
            if (delta == 0 && walking == 0 && swimming == 0)
                return;
            await Gameplay.AddConditionAsync(UserId.Value, walking, swimming, delta, ct);
            _savedPlaySeconds = total;
        }
        finally { _conditionSave.Release(); }
    }

    public async Task SendAsync(IOutgoingPacket packet, CancellationToken ct = default)
    {
        await SendAsync(packet.Type, packet.ToBytes(), ct);
    }

    public async Task SendAsync(PacketType type, byte[] body, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _sentCount);
        await _send(type, body, ct);
    }

    public (byte Day, byte Hour, byte Minute) GameClock()
    {
        _clockOrigin ??= Zone.Time.GetTimestamp();
        var elapsedSeconds = Zone.Time.GetElapsedTime(_clockOrigin.Value).TotalSeconds;
        var total = (12 * 60) + (int)(elapsedSeconds / 1.25);
        var day = total / (24 * 60);
        if (day > 0xFF)
            throw new InvalidDataException("game clock fields out of range");
        return ((byte)day, (byte)((total / 60) % 24), (byte)(total % 60));
    }

    public void EnsureZonePacket(PacketType opcode)
    {
        if (UserId is null || (State != SessionState.CharacterCreated && State != SessionState.ZoneEntered))
            throw new InvalidDataException($"zone packet 0x{(uint)opcode:X} before character assignment");
    }

    public uint AssignCharacter(int databaseId)
    {
        // Keep existing small player ids; reserve the planted/entity id region.
        var preferred = databaseId is > 0 and < 1000 ? (uint)databaseId : 0;
        CharacterId = Zone.Register(ZoneEntityKind.Player, databaseId, PositionX, PositionY, preferred);
        Zone.Attach(this);
        return CharacterId.Value;
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        await _disconnect.WaitAsync(ct);
        try
        {
            if (_disconnected) return;
            try
            {
                if (Kind == ServerKind.Zone && State == SessionState.ZoneEntered && UserId is not null)
                    await SaveConditionAsync(ct: ct);
            }
            finally
            {
                _disconnected = true;
                Zone.Detach(this);
                State = SessionState.Disconnected;
                if (_close is not null) await _close(ct);
            }
        }
        finally { _disconnect.Release(); }
    }
}

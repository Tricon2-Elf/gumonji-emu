using System.Diagnostics;
using gumonji.Network;

namespace gumonji.Common;

public sealed record EmuOptions
{
    public string BindAddress { get; init; } = "0.0.0.0";
    public int FemsgPort { get; init; } = 12421;
    public int GamePort { get; init; } = 23432;
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
    public const string CharacterCreationMenu = "CHARACTER_CREATION_MENU";
    public const string CharacterCreated = "CHARACTER_CREATED";
    public const string ZoneEntered = "ZONE_ENTERED";
}

public sealed record ActionEmoteState(uint ActionId, byte Sequence);

public sealed record ChatState(byte[] Sender, byte[] Message);

public sealed class GumonjiSession
{
    private readonly Func<PacketType, byte[], CancellationToken, Task> _send;
    private long? _clockOrigin;

    public GumonjiSession(
        ServerKind kind,
        Accounts.LocalAccounts accounts,
        EmuOptions options,
        Func<PacketType, byte[], CancellationToken, Task> send)
    {
        Kind = kind;
        Accounts = accounts;
        Options = options;
        _send = send;
        State = kind == ServerKind.Femsg ? SessionState.WaitLogin : SessionState.WaitCheckPassword;
    }

    public ServerKind Kind { get; }
    public Accounts.LocalAccounts Accounts { get; }
    public EmuOptions Options { get; }
    public uint? UserId { get; set; }
    public uint? CharacterId { get; set; }
    public string State { get; set; }
    public bool SilentNoReply { get; set; }
    public int SentCount { get; private set; }
    public HashSet<(uint X, uint Y)> PlantedChunks { get; } = [];
    public ActionEmoteState? LastActionEmote { get; set; }
    public uint? LastFacialEmoteId { get; set; }
    public ChatState? LastChat { get; set; }
    public bool IsTypingInChat { get; set; }
    public uint PositionX { get; set; } = 64000;
    public uint PositionY { get; set; } = 64000;
    private long? _playStarted;
    private long _savedPlaySeconds;

    public void StartPlayTime() => _playStarted ??= Stopwatch.GetTimestamp();

    public async Task SaveConditionAsync(uint walking = 0, uint swimming = 0, CancellationToken ct = default)
    {
        if (State != SessionState.ZoneEntered || UserId is null)
            throw new InvalidDataException("condition report before zone entry");
        StartPlayTime();
        var total = (long)Stopwatch.GetElapsedTime(_playStarted!.Value).TotalSeconds;
        var delta = total - _savedPlaySeconds;
        if (delta == 0 && walking == 0 && swimming == 0)
            return;
        await Accounts.Gameplay.AddConditionAsync(UserId.Value, walking, swimming, delta, ct);
        _savedPlaySeconds = total;
    }

    public async Task SendAsync(IOutgoingPacket packet, CancellationToken ct = default)
    {
        await SendAsync(packet.Type, packet.ToBytes(), ct);
    }

    public async Task SendAsync(PacketType type, byte[] body, CancellationToken ct = default)
    {
        SentCount++;
        await _send(type, body, ct);
    }

    public (byte Day, byte Hour, byte Minute) GameClock()
    {
        _clockOrigin ??= Stopwatch.GetTimestamp();
        var elapsedSeconds = (Stopwatch.GetTimestamp() - _clockOrigin.Value) / (double)Stopwatch.Frequency;
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
}

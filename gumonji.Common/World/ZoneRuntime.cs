using System.Collections.Concurrent;

namespace gumonji.Common.World;

public enum ZoneEntityKind { Player, Animal, Plant, Item, Door, House }

public sealed record ZoneEntity(uint Id, ZoneEntityKind Kind, long DatabaseId, uint X, uint Y);

/// <summary>Shared authority for a single zone; a host injects one instance into its sessions.</summary>
public sealed class ZoneRuntime
{
    private readonly object _sync = new();
    private readonly SemaphoreSlim _mutations = new(1, 1);
    private readonly Dictionary<uint, ZoneEntity> _entities = [];
    private readonly Dictionary<(ZoneEntityKind Kind, long Id), uint> _identities = [];
    private readonly HashSet<uint> _allocatedIds = [];
    private readonly ConcurrentDictionary<GumonjiSession, byte> _sessions = new();
    private uint _nextId = 0x10000000;
    private bool _vehiclePickedUp;
    private long _lastAnimalStep;
    private readonly Action<string>? _audit;

    public ZoneRuntime(TimeProvider? time = null, Random? random = null, Action<string>? audit = null)
    {
        Time = time ?? TimeProvider.System;
        Random = random ?? Random.Shared;
        _audit = audit;
        _lastAnimalStep = Time.GetTimestamp();
        Register(ZoneEntityKind.Animal, SpawnActors.CowId, 69000, 66000, SpawnActors.CowId);
        Register(ZoneEntityKind.Item, SpawnActors.CarId,
            SpawnActors.CarX * 1000u, SpawnActors.CarY * 1000u, SpawnActors.CarId);
    }

    public TimeProvider Time { get; }
    public Random Random { get; }
    public int SessionCount => _sessions.Count;

    public void ReportEnvironment(uint userId, EnvironmentReportRequest report)
    {
        static string Field(byte[] value)
        {
            var end = Array.IndexOf(value, (byte)0);
            return System.Text.Encoding.Latin1.GetString(value.AsSpan(0, end < 0 ? value.Length : end))
                .Replace(':', '_');
        }
        _audit?.Invoke($"environment user={userId}:os={Field(report.Os)}:cpu={Field(report.Cpu)}:" +
            $"graphics_card={Field(report.GraphicsCard)}:graphics_driver={Field(report.GraphicsDriver)}:" +
            $"vram={report.VideoMemory}:timezone={Field(report.Timezone)}:mainmem={report.MainMemory}:looptest={report.LoopTest}");
    }

    public uint Register(ZoneEntityKind kind, long databaseId, uint x, uint y, uint preferredId = 0)
    {
        lock (_sync)
        {
            var key = (kind, databaseId);
            if (!_identities.TryGetValue(key, out var id))
            {
                id = preferredId;
                if (id == 0 || id > 0x7FFFFFFF || _allocatedIds.Contains(id))
                {
                    while (_allocatedIds.Contains(_nextId) && _nextId <= 0x7FFFFFFF)
                        _nextId++;
                    if (_nextId > 0x7FFFFFFF)
                        throw new InvalidOperationException("zone entity ids exhausted");
                    id = _nextId++;
                }
                _identities.Add(key, id);
                _allocatedIds.Add(id);
            }
            _entities[id] = new(id, kind, databaseId, x, y);
            return id;
        }
    }

    public ZoneEntity? Find(uint id)
    {
        lock (_sync)
            return _entities.GetValueOrDefault(id);
    }

    public ZoneEntity[] Snapshot()
    {
        lock (_sync)
            return _entities.Values.OrderBy(e => e.Id).ToArray();
    }

    public void Move(uint id, uint x, uint y)
    {
        lock (_sync)
            if (_entities.TryGetValue(id, out var entity))
                _entities[id] = entity with { X = x, Y = y };
    }

    public bool VehiclePickedUp
    {
        get { lock (_sync) return _vehiclePickedUp; }
        set
        {
            lock (_sync)
            {
                _vehiclePickedUp = value;
                if (value) _entities.Remove(SpawnActors.CarId);
                else _entities[SpawnActors.CarId] = new(SpawnActors.CarId, ZoneEntityKind.Item,
                    SpawnActors.CarId, SpawnActors.CarX * 1000u, SpawnActors.CarY * 1000u);
            }
        }
    }

    public void Attach(GumonjiSession session) => _sessions.TryAdd(session, 0);

    public void Detach(GumonjiSession session)
    {
        _sessions.TryRemove(session, out _);
        lock (_sync)
            if (session.CharacterId is { } id && _entities.TryGetValue(id, out var entity) &&
                entity.Kind == ZoneEntityKind.Player && !_sessions.Keys.Any(s => s.CharacterId == id))
                _entities.Remove(id);
    }

    public GumonjiSession? FindPlayer(uint id) =>
        _sessions.Keys.FirstOrDefault(s => s.CharacterId == id &&
            s.State is SessionState.CharacterCreated or SessionState.ZoneEntered);

    public async Task SerializeAsync(Func<Task> mutation, CancellationToken ct = default)
    {
        await _mutations.WaitAsync(ct);
        try { await mutation(); }
        finally { _mutations.Release(); }
    }

    public async Task BroadcastAsync(IOutgoingPacket packet, uint x, uint y,
        GumonjiSession? except = null, CancellationToken ct = default)
    {
        var chunk = (x / PlantWorld.PageEdge / 1000, y / PlantWorld.PageEdge / 1000);
        foreach (var session in _sessions.Keys)
        {
            if (session == except || session.State != SessionState.ZoneEntered ||
                !session.PlantedChunks.Contains(chunk)) continue;
            try { await session.SendAsync(packet, ct); }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                // The committed world mutation must never be replayed for a failed recipient.
                await session.DisconnectAsync(CancellationToken.None);
            }
        }
    }

    public bool StepAnimal()
    {
        lock (_sync)
        {
            var now = Time.GetTimestamp();
            if (Time.GetElapsedTime(_lastAnimalStep, now) < TimeSpan.FromSeconds(1)) return false;
            _lastAnimalStep = now;
            if (!_entities.TryGetValue(SpawnActors.CowId, out var cow)) return false;
            var step = Random.Next(4);
            _entities[cow.Id] = cow with
            {
                X = (uint)Math.Clamp((long)cow.X + (step == 0 ? -1000 : step == 1 ? 1000 : 0), 0, 159000),
                Y = (uint)Math.Clamp((long)cow.Y + (step == 2 ? -1000 : step == 3 ? 1000 : 0), 0, 159000),
            };
            return true;
        }
    }
}

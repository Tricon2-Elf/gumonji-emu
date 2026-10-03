using gumonji.Common.DAL;
using gumonji.Common.DAL.Entities;
using gumonji.Common.DAL.Repositories;
using gumonji.Common.World;
using gumonji.Network;
using gumonji.Network.Packets.Zone;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace gumonji.Common.Tests;

public sealed class ZoneSessionPacketTests
{
    private static readonly PacketDispatcher Dispatcher = TestPacketDispatcher.Create();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(128)]
    public void EnvironmentPreservesRawBytesAndFieldOrder(int size)
    {
        var field = Enumerable.Repeat((byte)0xff, size).ToArray();
        var payload = EnvironmentBytes(field);
        var report = EnvironmentReportRequest.FromBytes(payload);
        Assert.Equal(field, report.Os);
        Assert.Equal("cpu"u8.ToArray(), report.Cpu);
        Assert.Equal("gpu"u8.ToArray(), report.GraphicsCard);
        Assert.Equal("driver"u8.ToArray(), report.GraphicsDriver);
        Assert.Equal(0x12345678u, report.VideoMemory);
        Assert.Equal("tz"u8.ToArray(), report.Timezone);
        Assert.Equal(0x87654321u, report.MainMemory);
        Assert.Equal(0xabcdef01u, report.LoopTest);
        Assert.Throws<InvalidDataException>(() => EnvironmentReportRequest.FromBytes(payload[..^1]));
        Assert.Throws<InvalidDataException>(() => EnvironmentReportRequest.FromBytes([..payload, 0]));
    }

    [Theory]
    [InlineData("81")]
    [InlineData("fd00000081")]
    [InlineData("fdffffffff")]
    [InlineData("fd00")]
    [InlineData("fe")]
    public void EnvironmentRejectsInvalidCompactFields(string hex) =>
        Assert.Throws<InvalidDataException>(() => EnvironmentReportRequest.FromBytes(Convert.FromHexString(hex)));

    [Fact]
    public void EnvironmentAcceptsOriginalExtendedCountEncoding()
    {
        var payload = EnvironmentBytes(new byte[128]);
        var extended = new byte[] { 0xfd, 0, 0, 0, 128 }.Concat(payload.Skip(1)).ToArray();
        Assert.Equal(128, EnvironmentReportRequest.FromBytes(extended).Os.Length);
    }

    [Theory]
    [InlineData(PacketType.LogoutRequest)]
    [InlineData(PacketType.RenderTaskRequest)]
    [InlineData(PacketType.EntityPositionRequest)]
    [InlineData(PacketType.TakeoffRequest)]
    [InlineData(PacketType.EnvironmentReportRequest)]
    [InlineData(PacketType.NearestCharacterRequest)]
    public async Task NewRequestsRegisterOnlyOnZone(PacketType opcode)
    {
        using var db = new Database();
        var player = await db.Player(new ZoneRuntime(), "registered", 0, 0);
        var body = opcode switch
        {
            PacketType.EntityPositionRequest => U32(player.Session.CharacterId!.Value),
            PacketType.NearestCharacterRequest => new byte[8],
            PacketType.EnvironmentReportRequest => EnvironmentBytes([]),
            _ => Array.Empty<byte>(),
        };
        Assert.False(await Dispatcher.DispatchAsync(ServerKind.Femsg, opcode, body, player.Session));
        Assert.False(await Dispatcher.DispatchAsync(ServerKind.Backd, opcode, body, player.Session));
        Assert.Empty(player.Sent);
        await Dispatch(player, opcode, body);
    }

    [Fact]
    public void RequestSizesAreExact()
    {
        foreach (var size in new[] { 1, 4, 8 })
        {
            Assert.Throws<InvalidDataException>(() => LogoutRequest.FromBytes(new byte[size]));
            Assert.Throws<InvalidDataException>(() => TakeoffRequest.FromBytes(new byte[size]));
            Assert.Throws<InvalidDataException>(() => RenderTaskRequest.FromBytes(new byte[size]));
        }
        foreach (var size in new[] { 0, 3, 5 })
            Assert.Throws<InvalidDataException>(() => EntityPositionRequest.FromBytes(new byte[size]));
        foreach (var size in new[] { 0, 7, 9 })
            Assert.Throws<InvalidDataException>(() => NearestCharacterRequest.FromBytes(new byte[size]));
        Assert.Equal(0x12345678u, EntityPositionRequest.FromBytes(Convert.FromHexString("12345678")).EntityId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(48)]
    public void RenderWireHasParallelArraysAndAccountIdentity(int count)
    {
        var items = Enumerable.Range(0, count).Select(i => new RenderItem((byte)i, (ushort)(0x1200 + i), 2, 3)).ToArray();
        var bytes = new RenderTaskResponse(0x12345678, 1, 16, 7, 4, items).ToBytes();
        Assert.Equal(17 + count * 5, bytes.Length);
        var reader = new PacketReader(bytes);
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal(new byte[] { 1, 1, 16, 1 }, reader.ReadBytes(4).ToArray());
        Assert.Equal(0x12345678u, reader.ReadUInt32());
        Assert.Equal(4, reader.ReadByte());
        Assert.Equal(items.Select(i => i.Slot), reader.ReadCompactBytes());
        Assert.Equal(count, reader.ReadCompactLength());
        foreach (var item in items) Assert.Equal(item.Type, reader.ReadUInt16());
        Assert.Equal(items.Select(i => i.Subtype), reader.ReadCompactBytes());
        Assert.Equal(items.Select(i => i.Color), reader.ReadCompactBytes());
        reader.ExpectEnd();
        Assert.Throws<InvalidDataException>(() => new RenderTaskResponse(1, 0, 0, 0, 0, [new(48, 1, 0, 0)]).ToBytes());
        Assert.Throws<InvalidDataException>(() => new RenderTaskResponse(1, 0, 0, 0, 0, [new(0, 1, 0, 0), new(0, 2, 0, 0)]).ToBytes());
    }

    [Fact]
    public async Task CharacterQueriesReadOtherSessionsAndIgnoreDiscardedFields()
    {
        using var db = new Database();
        var world = new ZoneRuntime();
        var first = await db.Player(world, "first", 10, 20);
        var second = await db.Player(world, "second", 30, 40);
        var third = await db.Player(world, "third", 100, 100);
        await Dispatch(first, PacketType.EntityPositionRequest, U32(second.Session.CharacterId!.Value));
        Assert.Equal("00000000" + second.Session.CharacterId.Value.ToString("x8") + "0000001e00000028",
            Convert.ToHexString(Assert.Single(first.Sent).Body).ToLowerInvariant());
        first.Sent.Clear();
        await Dispatch(first, PacketType.NearestCharacterRequest, Convert.FromHexString("ffffffff12345678"));
        Assert.Equal("00000000" + second.Session.UserId!.Value.ToString("x8"),
            Convert.ToHexString(Assert.Single(first.Sent).Body).ToLowerInvariant());
        await second.Session.DisconnectAsync();
        await third.Session.DisconnectAsync();
        first.Sent.Clear();
        await Dispatch(first, PacketType.NearestCharacterRequest, new byte[8]);
        Assert.Equal("ffffffe500000000", Convert.ToHexString(Assert.Single(first.Sent).Body).ToLowerInvariant());
        foreach (var id in new[] { 0u, SpawnActors.CowId, SpawnActors.CarId, second.Session.CharacterId.Value })
        {
            first.Sent.Clear();
            await Dispatch(first, PacketType.EntityPositionRequest, U32(id));
            Assert.Equal("ffffffff" + id.ToString("x8") + "0000000000000000",
                Convert.ToHexString(Assert.Single(first.Sent).Body).ToLowerInvariant());
        }
    }

    [Fact]
    public async Task TakeoffIsSilentAndDoesNotUnequipAndRenderUsesSavedAppearance()
    {
        using var db = new Database();
        var player = await db.Player(new ZoneRuntime(), "driver", 0, 0);
        var item = await db.Gameplay.EnsureStarterVehicleAsync(player.Session.UserId!.Value);
        player.Session.EquippedVehicleId = (uint)item!.Id;
        await Dispatch(player, PacketType.TakeoffRequest, []);
        Assert.Empty(player.Sent);
        Assert.True(player.Session.SilentNoReply);
        Assert.Equal((uint)item.Id, player.Session.EquippedVehicleId);
        await Dispatch(player, PacketType.RenderTaskRequest, []);
        Assert.Equal(PacketType.RenderTaskResponse, Assert.Single(player.Sent).Type);
        Assert.Equal(new RenderTaskResponse(player.Session.UserId.Value, 1, 16, 7, 0,
            [new((byte)item.Slot, (ushort)item.ItemType, (byte)item.Subtype, (byte)item.Color)]).ToBytes(), player.Sent[0].Body);
        var unauthenticated = new GumonjiSession(ServerKind.Zone, db.Accounts, db.Characters, db.LoginTokens, db.Gameplay, new(), (_, _, _) => Task.CompletedTask);
        await Dispatcher.DispatchAsync(ServerKind.Zone, PacketType.TakeoffRequest, ReadOnlyMemory<byte>.Empty, unauthenticated);
        await Assert.ThrowsAsync<InvalidDataException>(() => Dispatcher.DispatchAsync(ServerKind.Zone,
            PacketType.RenderTaskRequest, ReadOnlyMemory<byte>.Empty, unauthenticated));
        await Assert.ThrowsAsync<InvalidDataException>(() => Dispatcher.DispatchAsync(ServerKind.Zone,
            PacketType.EntityPositionRequest, U32(1), unauthenticated));
    }

    [Fact]
    public async Task NearestCharacterUsesOriginalTruncatedDistanceTies()
    {
        using var db = new Database();
        var world = new ZoneRuntime();
        var first = await db.Player(world, "first", 0, 0);
        var farther = await db.Player(world, "farther", 1000, 20);
        await db.Player(world, "closer", 1000, 10);
        await Dispatch(first, PacketType.NearestCharacterRequest, new byte[8]);
        // Both distances truncate to 1000; original keeps the first registry entry.
        Assert.Equal(new NearestCharacterResponse(0, farther.Session.UserId!.Value).ToBytes(), Assert.Single(first.Sent).Body);
    }

    [Fact]
    public async Task EnvironmentLogsOriginalEscapingWithoutModifyingRawReport()
    {
        using var db = new Database();
        var log = new List<string>();
        var player = await db.Player(new ZoneRuntime(audit: log.Add), "reporter", 0, 0);
        var raw = new byte[] { 0xff, (byte)':', 0, (byte)'x' };
        await Dispatch(player, PacketType.EnvironmentReportRequest, EnvironmentBytes(raw));
        Assert.Empty(player.Sent);
        Assert.True(player.Session.SilentNoReply);
        Assert.Equal(raw, player.Session.LastEnvironmentReport!.Os);
        Assert.Contains(":os=ÿ_:cpu=cpu:", Assert.Single(log));
    }

    [Fact]
    public async Task ConcurrentDisconnectAndConditionSavePersistElapsedTimeOnce()
    {
        using var db = new Database();
        var clock = new Clock();
        var world = new ZoneRuntime(clock);
        var closed = 0;
        var player = await db.Player(world, "logout", 0, 0, () => { Interlocked.Increment(ref closed); return Task.CompletedTask; });
        player.Session.StartPlayTime();
        clock.Advance(TimeSpan.FromSeconds(42));
        await Task.WhenAll(player.Session.SaveConditionAsync(), player.Session.SaveConditionAsync());
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => player.Session.DisconnectAsync()));
        Assert.Equal(1, closed);
        Assert.Equal(SessionState.Disconnected, player.Session.State);
        Assert.Equal(0, world.SessionCount);
        Assert.Null(world.Find(player.Session.CharacterId!.Value));
        Assert.Equal(42, (await db.Characters.GetByUserIdAsync(player.Session.UserId!.Value))!.PlayedSeconds);
        await Dispatch(player, PacketType.LogoutRequest, []);
        Assert.Equal(1, closed);
        Assert.Empty(player.Sent);
    }

    [Fact]
    public async Task SharedRuntimeAllocatesDistinctWireIdsAndSerializesMutations()
    {
        var world = new ZoneRuntime();
        var ids = Enumerable.Range(0, 100).AsParallel().Select(i => world.Register(ZoneEntityKind.Player,
            10000 + i, 0, 0, SpawnActors.CowId)).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.All(ids, id => Assert.InRange(id, 1u, 0x7fffffffu));
        Assert.DoesNotContain(SpawnActors.CowId, ids);
        var value = 0;
        await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => world.SerializeAsync(async () =>
        {
            var old = value;
            await Task.Yield();
            value = old + 1;
        })));
        Assert.Equal(100, value);
    }

    [Fact]
    public async Task DetachedWireIdentityCannotBeReassignedToAnotherEntity()
    {
        using var db = new Database();
        var world = new ZoneRuntime();
        var player = await db.Player(world, "returning", 0, 0);
        var id = player.Session.CharacterId!.Value;
        var databaseId = (await db.Characters.GetByUserIdAsync(player.Session.UserId!.Value))!.Id;
        await player.Session.DisconnectAsync();
        var itemId = world.Register(ZoneEntityKind.Item, 9999, 1, 2, id);
        Assert.NotEqual(id, itemId);
        Assert.Equal(id, world.Register(ZoneEntityKind.Player, databaseId, 3, 4, id));
        Assert.Equal(ZoneEntityKind.Item, world.Find(itemId)!.Kind);
    }

    [Fact]
    public async Task BroadcastSelectsSubscribersAndFailedSendDoesNotRepeatTheMutation()
    {
        using var db = new Database();
        var world = new ZoneRuntime();
        var sender = await db.Player(world, "sender", 64000, 64000);
        var observer = await db.Player(world, "observer", 64000, 64000);
        var distant = await db.Player(world, "distant", 0, 0);
        sender.Session.PlantedChunks.Add((2, 2));
        observer.Session.PlantedChunks.Add((2, 2));
        distant.Session.PlantedChunks.Add((0, 0));
        var broken = new GumonjiSession(ServerKind.Zone, db.Accounts, db.Characters, db.LoginTokens, db.Gameplay, new(), (_, _, _) => throw new IOException("closed"), world)
            { State = SessionState.ZoneEntered };
        broken.PlantedChunks.Add((2, 2)); world.Attach(broken);
        var committed = 0;
        await world.SerializeAsync(async () =>
        {
            committed++;
            world.Move(SpawnActors.CowId, 65000, 65000);
            await world.BroadcastAsync(new AnimalPlaceResponse(SpawnActors.CowId, 65, 65), 65000, 65000, sender.Session);
        });
        Assert.Equal(1, committed);
        Assert.Empty(sender.Sent);
        Assert.Empty(distant.Sent);
        Assert.Equal(PacketType.AnimalPlaceResponse, Assert.Single(observer.Sent).Type);
        Assert.Equal(SessionState.Disconnected, broken.State);
        Assert.Equal(65000u, world.Find(SpawnActors.CowId)!.X);
    }

    private static byte[] U32(uint value) { var writer = new PacketWriter(); writer.Write(value); return writer.ToBytes(); }
    private static byte[] EnvironmentBytes(byte[] os)
    {
        var writer = new PacketWriter();
        foreach (var bytes in new[] { os, "cpu"u8.ToArray(), "gpu"u8.ToArray(), "driver"u8.ToArray() }) writer.WriteCompactBytes(bytes);
        writer.Write(0x12345678u); writer.WriteCompactBytes("tz"u8); writer.Write(0x87654321u); writer.Write(0xabcdef01u);
        return writer.ToBytes();
    }
    private static async Task Dispatch(Player player, PacketType opcode, byte[] bytes) =>
        Assert.True(await Dispatcher.DispatchAsync(ServerKind.Zone, opcode, bytes, player.Session));
    private sealed record Player(GumonjiSession Session, List<(PacketType Type, byte[] Body)> Sent);
    private sealed class Clock : TimeProvider
    {
        private long _ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _ticks;
        public void Advance(TimeSpan value) => _ticks += value.Ticks;
    }
    private sealed class Database : IDbContextFactory<MainContext>, IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), $"gumonji-session-test-{Guid.NewGuid():N}.db");
        private readonly DbContextOptions<MainContext> _options;
        public IAccountRepository Accounts { get; }
        public ICharacterRepository Characters { get; }
        public ILoginTokenRepository LoginTokens { get; }
        public IGameplayRepository Gameplay { get; }
        public Database()
        {
            _options = new DbContextOptionsBuilder<MainContext>().UseSqlite($"Data Source={_path};Pooling=False").Options;
            using var db = CreateDbContext(); db.Database.Migrate();
            Accounts = new AccountRepository(this);
            Characters = new CharacterRepository(this);
            LoginTokens = new LoginTokenRepository(this);
            Gameplay = new GameplayRepository(this);
        }
        public MainContext CreateDbContext() => new(_options);
        public async Task<Player> Player(ZoneRuntime world, string name, uint x, uint y, Func<Task>? close = null)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(name);
            var uid = await Accounts.GetOrCreateAsync(bytes, "password"u8.ToArray());
            await Characters.SaveAsync(uid, new Character { Name = bytes, Body = 1, Model = 16, Style = 7 });
            var sent = new List<(PacketType Type, byte[] Body)>();
            var session = new GumonjiSession(ServerKind.Zone, Accounts, Characters, LoginTokens, Gameplay, new(), (type, body, _) =>
            { sent.Add((type, body)); return Task.CompletedTask; }, world, _ => close?.Invoke() ?? Task.CompletedTask)
            { UserId = uid, State = SessionState.ZoneEntered, PositionX = x, PositionY = y };
            session.AssignCharacter((await Characters.GetByUserIdAsync(uid))!.Id);
            return new(session, sent);
        }
        public void Dispose() { File.Delete(_path); File.Delete(_path + "-wal"); File.Delete(_path + "-shm"); }
    }
}

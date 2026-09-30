using gumonji.Common.Accounts;
using gumonji.Common.DAL;
using gumonji.Common.DAL.Entities;
using gumonji.Common.DAL.Repositories;
using gumonji.Common.World;
using gumonji.Network;
using gumonji.Network.Packets.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace gumonji.Common.Tests;

public sealed class GameplayTests
{
    [Fact]
    public void ConditionWireLayoutMatchesClientParser()
    {
        var packet = new CharacterConditionResponse("Alice"u8.ToArray(), 7200, 1230, 450);
        Assert.Equal(0x868u, (uint)packet.Type);
        var reader = new PacketReader(packet.ToBytes());
        Assert.Equal("Alice"u8.ToArray(), reader.ReadCompactBytes());
        var fields = Enumerable.Range(0, 14).Select(_ => 0u).ToArray();
        for (var i = 0; i < fields.Length; i++) fields[i] = reader.ReadUInt32();
        Assert.Equal(new uint[] { 0, 0, 0, 0, 7200, 1230, 450, 0, 0, 0, 0, 0, 0, 0 }, fields);
        Assert.Equal("Alice"u8.ToArray(), reader.ReadCompactBytes());
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal(0u, reader.ReadUInt32());
        reader.ExpectEnd();
    }

    [Fact]
    public void InventoryWireLayoutMatchesClientParser()
    {
        var packet = new InventorySlotResponse(2, 99, 92, 5, 8, 200);
        Assert.Equal(0x3C0u, (uint)packet.Type);
        var reader = new PacketReader(packet.ToBytes());
        Assert.Equal(2, reader.ReadByte());
        Assert.Equal(99u, reader.ReadUInt32());
        Assert.Equal(92, reader.ReadUInt16());
        Assert.Equal(5, reader.ReadByte());
        Assert.Equal(8, reader.ReadByte());
        Assert.Equal(200u, reader.ReadUInt32());
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal(8, reader.ReadCompactLength());
        for (var i = 0; i < 8; i++) Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal(8, reader.ReadCompactLength());
        for (var i = 0; i < 8; i++) Assert.Empty(reader.ReadCompactBytes());
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Empty(reader.ReadCompactBytes());
        reader.ExpectEnd();
    }

    [Fact]
    public async Task StarterTruckUseEquipsVehicleInAvatar()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        var first = await fixture.Repository.EnsureStarterVehicleAsync(uid);
        Assert.NotNull(first);
        Assert.Equal(first.Id, (await fixture.Repository.EnsureStarterVehicleAsync(uid))!.Id);
        Assert.Single(await fixture.Repository.GetInventoryAsync(uid));

        var sent = new List<(PacketType Type, byte[] Body)>();
        var session = await fixture.Session(uid, sent);
        var request = new PacketWriter();
        request.Write((uint)first.Slot);
        request.Write(0u);
        request.Write((ushort)64);
        request.Write((ushort)64);
        request.Write(0u);
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);
        Assert.True(await dispatcher.DispatchAsync(ServerKind.Game, PacketType.ItemUseRequest,
            request.ToBytes(), session));
        Assert.Equal(new[] { PacketType.ItemUseResponse, PacketType.CharacterAvatarResponse },
            sent.Select(p => p.Type));
        var result = new PacketReader(sent[0].Body);
        Assert.Equal(0u, result.ReadUInt32());
        Assert.Equal(0u, result.ReadUInt32());
        Assert.Equal((uint)first.Slot, result.ReadUInt32());
        result.ExpectEnd();

        var avatar = new PacketReader(sent[1].Body);
        Assert.Equal(0u, avatar.ReadUInt32());
        Assert.Equal(session.CharacterId, avatar.ReadUInt32());
        for (var i = 0; i < 4; i++) avatar.ReadByte();
        Assert.Equal("Alice"u8.ToArray(), avatar.ReadCompactBytes());
        avatar.ReadUInt32();
        avatar.ReadByte();
        avatar.ReadUInt32();
        avatar.ReadUInt32();
        Assert.Equal(0, avatar.ReadCompactLength());
        Assert.Equal(0, avatar.ReadCompactLength());
        avatar.ReadByte();
        Assert.Equal([(byte)first.Slot], avatar.ReadCompactBytes());
        Assert.Equal(1, avatar.ReadCompactLength());
        Assert.Equal(ItemTemplateIds.ToyCar, avatar.ReadUInt16());
        Assert.Equal([0], avatar.ReadCompactBytes());
        Assert.Equal([0], avatar.ReadCompactBytes());
        Assert.Equal(1, avatar.ReadCompactLength());
        Assert.Equal((uint)first.Id, avatar.ReadUInt32());
        Assert.Equal(8, avatar.ReadCompactLength());
        for (var i = 0; i < 8; i++) Assert.Equal(0u, avatar.ReadUInt32());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void HarvestRejectsMalformedPayload(int length) =>
        Assert.Throws<InvalidDataException>(() => PlantHarvestRequest.FromBytes(new byte[length]));

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(13)]
    public void TotalsRejectMalformedPayload(int length) =>
        Assert.Throws<InvalidDataException>(() => MovementTotalsRequest.FromBytes(new byte[length]));

    [Fact]
    public async Task ConditionPersistsAndAnswersThroughDispatcher()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        var sent = new List<(PacketType Type, byte[] Body)>();
        var session = await fixture.Session(uid, sent);
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);
        var totals = new PacketWriter();
        totals.Write(uint.MaxValue); // untrusted field cannot select another account
        totals.Write(1230u);
        totals.Write(450u);
        Assert.True(await dispatcher.DispatchAsync(ServerKind.Game, PacketType.MovementTotalsRequest, totals.ToBytes(), session));
        Assert.Empty(sent);
        Assert.True(session.SilentNoReply);
        await fixture.Repository.AddConditionAsync(uid, 10, 20, 7200);
        Assert.True(await dispatcher.DispatchAsync(ServerKind.Game, PacketType.CharacterConditionRequest, ReadOnlyMemory<byte>.Empty, session));
        var reply = Assert.Single(sent);
        Assert.Equal(PacketType.CharacterConditionResponse, reply.Type);
        var reader = new PacketReader(reply.Body);
        Assert.Equal("Alice"u8.ToArray(), reader.ReadCompactBytes());
        for (var i = 0; i < 4; i++) reader.ReadUInt32();
        Assert.True(reader.ReadUInt32() >= 7200);
        Assert.Equal(1240u, reader.ReadUInt32());
        Assert.Equal(470u, reader.ReadUInt32());
        await using var reopened = fixture.CreateDbContext();
        var saved = await reopened.Characters.SingleAsync();
        Assert.Equal(1240, saved.WalkingDistance);
        Assert.Equal(470, saved.SwimmingDistance);
        Assert.Throws<InvalidDataException>(() => CharacterConditionRequest.FromBytes([1]));
    }

    [Fact]
    public async Task HarvestUpdatesInventoryAndTreeAndRestoresOnReconnect()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        var sent = new List<(PacketType Type, byte[] Body)>();
        var session = await fixture.Session(uid, sent);
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);
        var position = new PacketWriter();
        position.Write(70000u);
        position.Write(69000u);
        await dispatcher.DispatchAsync(ServerKind.Game, PacketType.PositionReportRequest, position.ToBytes(), session);
        var harvest = new PacketWriter();
        harvest.Write(1000u);
        Assert.True(await dispatcher.DispatchAsync(ServerKind.Game, PacketType.PlantHarvestRequest, harvest.ToBytes(), session));
        Assert.Equal(new[] { PacketType.InventorySlotResponse, PacketType.PlantPlaceResponse, PacketType.ChatEventResponse },
            sent.Select(p => p.Type));
        var itemPacket = sent[0].Body;
        var plantPacket = sent[1].Body;
        Assert.Equal(0, plantPacket[24]); // stage after object/type/colour and four uints
        var item = Assert.Single(await fixture.Repository.GetInventoryAsync(uid), i => i.ItemType == 92);
        Assert.Equal(92, item.ItemType);
        Assert.Equal(5, item.Subtype);
        Assert.Equal(200, item.Fertility);
        var plant = await fixture.Repository.GetPlantAsync(1, 1000);
        Assert.Equal(35800, plant!.Fertility);
        Assert.Equal(0, plant.Stage);

        sent.Clear();
        var reconnect = await fixture.Session(uid, sent);
        var enter = new PacketWriter();
        enter.WriteCompactBytes([]);
        enter.WriteCompactBytes([]);
        enter.Write(1u);
        await dispatcher.DispatchAsync(ServerKind.Game, PacketType.ZoneEnterRequest, enter.ToBytes(), reconnect);
        Assert.Contains(sent, p => p.Type == PacketType.InventorySlotResponse && p.Body.SequenceEqual(itemPacket));
        sent.Clear();
        var page = new PacketWriter();
        page.Write(2u);
        page.Write(2u);
        await dispatcher.DispatchAsync(ServerKind.Game, PacketType.GetPageDataRequest, page.ToBytes(), reconnect);
        Assert.Contains(sent, p => p.Type == PacketType.PlantPlaceResponse && p.Body.SequenceEqual(plantPacket));
    }

    [Fact]
    public async Task HarvestRejectsInvalidRangeAndFullInventoryWithoutDepletingTree()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        Assert.NotNull((await fixture.Repository.HarvestAsync(uid, 1, 999, 70000, 69000)).Error);
        Assert.NotNull((await fixture.Repository.HarvestAsync(uid, 1, 1000, uint.MaxValue, 69000)).Error);
        Assert.NotNull((await fixture.Repository.HarvestAsync(uid, 1, 1000, 74001, 69000)).Error);
        await using (var db = fixture.CreateDbContext())
        {
            var character = await db.Characters.SingleAsync();
            db.InventoryItems.AddRange(Enumerable.Range(0, 16).Select(i => new InventoryItem
            {
                CharacterId = character.Id, Slot = i, ItemType = 92, Subtype = 5, Color = 8, Fertility = 200,
            }));
            await db.SaveChangesAsync();
        }
        var full = await fixture.Repository.HarvestAsync(uid, 1, 1000, 70000, 69000);
        Assert.Equal("Your inventory is full.", full.Error);
        Assert.Null(await fixture.Repository.GetPlantAsync(1, 1000));
        Assert.Equal(16, (await fixture.Repository.GetInventoryAsync(uid)).Count);
    }

    [Fact]
    public async Task ConcurrentHarvestAwardsExactlyOnceAcrossRepositoryInstances()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
            new GameplayRepository(fixture).HarvestAsync(uid, 1, 1000, 70000, 69000))));
        Assert.Single(results, r => r.Item is not null);
        Assert.Single(await fixture.Repository.GetInventoryAsync(uid));
        Assert.NotNull((await fixture.Repository.HarvestAsync(uid, 1, 1000, 70000, 69000)).Error);
    }

    [Fact]
    public async Task NewRequestsRequireEnteredAuthenticatedCharacter()
    {
        using var fixture = new DatabaseFixture();
        var session = new GumonjiSession(ServerKind.Game, fixture.Accounts, new(), (_, _, _) => Task.CompletedTask);
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);
        await Assert.ThrowsAsync<InvalidDataException>(() => dispatcher.DispatchAsync(ServerKind.Game,
            PacketType.CharacterConditionRequest, ReadOnlyMemory<byte>.Empty, session));
        await Assert.ThrowsAsync<InvalidDataException>(() => dispatcher.DispatchAsync(ServerKind.Game,
            PacketType.PlantHarvestRequest, new byte[4], session));
        await Assert.ThrowsAsync<InvalidDataException>(() => dispatcher.DispatchAsync(ServerKind.Game,
            PacketType.MovementTotalsRequest, new byte[12], session));
    }

    [Fact]
    public async Task MigrationPreservesExistingAccountsAndCharacters()
    {
        using var fixture = new DatabaseFixture(migrate: false);
        await using var db = fixture.CreateDbContext();
        await db.GetService<IMigrator>().MigrateAsync("20260928054208_InitialCreate");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Users (Username, PasswordHash) VALUES ('legacy', 'hash')");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Characters (UserId, Name, Body, Model, Style, Color) VALUES (1, X'416C696365', 1, 2, 3, 4)");
        await db.Database.MigrateAsync();
        var character = await db.Characters.SingleAsync();
        Assert.Equal("Alice"u8.ToArray(), character.Name);
        Assert.Equal(4, character.Color);
        Assert.Equal(0, character.WalkingDistance);
        Assert.Empty(await db.InventoryItems.ToListAsync());
        Assert.Empty(await db.PlantStates.ToListAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task WorldToyCarPickupAcknowledgesAndEquipsSavedVehicle()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        var sent = new List<(PacketType Type, byte[] Body)>();
        var session = await fixture.Session(uid, sent);
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);
        var request = new PacketWriter();
        request.Write(3000u);

        Assert.True(await dispatcher.DispatchAsync(ServerKind.Game, PacketType.ItemPickupRequest,
            request.ToBytes(), session));
        Assert.Equal(new[] { PacketType.ItemPickupResponse, PacketType.ItemRemoveResponse, PacketType.InventorySlotResponse,
            PacketType.CharacterAvatarResponse }, sent.Select(p => p.Type));
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 0, 0x0B, 0xB8 }, sent[0].Body);
        Assert.NotNull(session.EquippedVehicleId);
        Assert.Single(await fixture.Repository.GetInventoryAsync(uid), i => i.ItemType == ItemTemplateIds.ToyCar);

        sent.Clear();
        session.PositionX = 100000;
        await dispatcher.DispatchAsync(ServerKind.Game, PacketType.ItemPickupRequest, request.ToBytes(), session);
        Assert.Single(sent);
        Assert.Equal(PacketType.ItemPickupResponse, sent[0].Type);
        Assert.NotEqual(0, sent[0].Body[3]);
        Assert.Throws<InvalidDataException>(() => ItemPickupRequest.FromBytes([0, 0, 0]));
    }

    [Fact]
    public async Task ExistingStarterTruckBecomesToyCarInSameSlot()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        await using (var db = fixture.CreateDbContext())
        {
            var character = await db.Characters.SingleAsync();
            db.InventoryItems.Add(new InventoryItem
            {
                CharacterId = character.Id, Slot = 4, ItemType = ItemTemplateIds.Truck,
                Subtype = 0, Color = 0, Fertility = 1000,
            });
            await db.SaveChangesAsync();
        }

        var car = await fixture.Repository.EnsureStarterVehicleAsync(uid);
        Assert.NotNull(car);
        Assert.Equal(4, car.Slot);
        Assert.Equal(ItemTemplateIds.ToyCar, car.ItemType);
        Assert.Single(await fixture.Repository.GetInventoryAsync(uid));
    }

    [Fact]
    public async Task AnimalBumpTracksClientMovementWithoutReplacingAnimal()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        var sent = new List<(PacketType Type, byte[] Body)>();
        var session = await fixture.Session(uid, sent);
        session.PlantedChunks.Add((2, 2));
        var request = new PacketWriter();
        request.Write(2000u);
        request.Write((ushort)84);
        request.Write((ushort)76);
        request.Write((byte)0);
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);

        Assert.True(await dispatcher.DispatchAsync(ServerKind.Game, PacketType.AnimalMoveRequest,
            request.ToBytes(), session));
        Assert.Equal((ushort)84, session.CowX);
        Assert.Equal((ushort)76, session.CowY);
        Assert.Empty(sent);
        Assert.True(session.SilentNoReply);
        Assert.Throws<InvalidDataException>(() => AnimalMoveRequest.FromBytes([0, 0, 0, 1]));
    }

    private sealed class DatabaseFixture : IDbContextFactory<MainContext>, IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), $"gumonji-gameplay-test-{Guid.NewGuid():N}.db");
        private readonly DbContextOptions<MainContext> _options;
        public LocalAccounts Accounts { get; }
        public GameplayRepository Repository { get; }

        public DatabaseFixture(bool migrate = true)
        {
            _options = new DbContextOptionsBuilder<MainContext>().UseSqlite($"Data Source={_path};Pooling=False").Options;
            using var db = CreateDbContext();
            if (migrate) db.Database.Migrate();
            Repository = new(this);
            Accounts = new(new AccountRepository(this), new CharacterRepository(this), new LoginTokenRepository(this), Repository);
        }

        public MainContext CreateDbContext() => new(_options);

        public async Task<uint> CreatePlayer()
        {
            var uid = await Accounts.LoginAsync("alice"u8.ToArray(), "password"u8.ToArray());
            await Accounts.SaveCharacterAsync(uid, new Character { Name = "Alice"u8.ToArray() });
            return uid;
        }

        public async Task<GumonjiSession> Session(uint uid, List<(PacketType Type, byte[] Body)> sent)
        {
            var character = await Accounts.GetCharacterAsync(uid);
            var session = new GumonjiSession(ServerKind.Game, Accounts, new(), (type, body, _) =>
            {
                sent.Add((type, body));
                return Task.CompletedTask;
            }) { UserId = uid, CharacterId = (uint)character!.Id, State = SessionState.ZoneEntered };
            session.StartPlayTime();
            return session;
        }

        public void Dispose()
        {
            // Only this fixture's unique temporary database; never the user's gumonji.db.
            File.Delete(_path);
            File.Delete(_path + "-wal");
            File.Delete(_path + "-shm");
        }
    }
}

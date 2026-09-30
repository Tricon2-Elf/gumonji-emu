using gumonji.Common.Accounts;
using gumonji.Common.DAL;
using gumonji.Common.DAL.Entities;
using gumonji.Common.DAL.Repositories;
using gumonji.Common.World;
using gumonji.Network;
using gumonji.Network.Packets.Femsg;
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
    public async Task TutorialCompletionIsAcknowledgedAndRestoredOnLogin()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);
        var sent = new List<(PacketType Type, byte[] Body)>();
        var session = new GumonjiSession(ServerKind.Femsg, fixture.Accounts, new(), (type, body, _) =>
        {
            sent.Add((type, body));
            return Task.CompletedTask;
        }) { UserId = uid, State = SessionState.HandoffIssued };
        var complete = new PacketWriter();
        complete.Write(uid);
        complete.Write(7u);

        Assert.True(await dispatcher.DispatchAsync(ServerKind.Femsg, PacketType.TutorialCompleteRequest,
            complete.ToBytes(), session));
        Assert.Equal(PacketType.TutorialCompleteResponse, Assert.Single(sent).Type);
        Assert.Equal("00000000", Convert.ToHexString(sent[0].Body).ToLowerInvariant());
        await dispatcher.DispatchAsync(ServerKind.Femsg, PacketType.TutorialCompleteRequest,
            complete.ToBytes(), session);
        await using (var db = fixture.CreateDbContext())
            Assert.Single(await db.TutorialCompletions.ToListAsync());

        var reopened = new LocalAccounts(new AccountRepository(fixture), new CharacterRepository(fixture),
            new LoginTokenRepository(fixture), new GameplayRepository(fixture));
        sent.Clear();
        var loginSession = new GumonjiSession(ServerKind.Femsg, reopened, new(), (type, body, _) =>
        {
            sent.Add((type, body));
            return Task.CompletedTask;
        });
        var login = new PacketWriter();
        login.WriteCompactBytes("alice"u8);
        login.WriteCompactBytes("password"u8);
        await dispatcher.DispatchAsync(ServerKind.Femsg, PacketType.LoginRequest, login.ToBytes(), loginSession);
        var response = Assert.Single(sent);
        Assert.Equal(PacketType.LoginAcceptResponse, response.Type);
        var reader = new PacketReader(response.Body);
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal(uid, reader.ReadUInt32());
        reader.ReadCompactBytes();
        reader.ReadUInt32();
        reader.ReadUInt32();
        Assert.Equal(1u, reader.ReadUInt32());
        var flags = reader.ReadCompactBytes();
        Assert.Equal(100, flags.Length);
        Assert.Equal(1, flags[7]);
        Assert.Equal(0, flags[6]);
        Assert.Equal(1u, reader.ReadUInt32());
        reader.ExpectEnd();

        Assert.Throws<InvalidDataException>(() => TutorialCompleteRequest.FromBytes([0, 0, 0, 1]));
        var invalidId = new PacketWriter();
        invalidId.Write(uid);
        invalidId.Write(100u);
        sent.Clear();
        await dispatcher.DispatchAsync(ServerKind.Femsg, PacketType.TutorialCompleteRequest,
            invalidId.ToBytes(), session);
        Assert.Equal("00000001", Convert.ToHexString(Assert.Single(sent).Body).ToLowerInvariant());
    }

    [Fact]
    public async Task SavedCharacterIsOfferedForLoadingOnNextLogin()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        var saved = await fixture.Accounts.GetCharacterAsync(uid);
        var reopenedAccounts = new LocalAccounts(new AccountRepository(fixture),
            new CharacterRepository(fixture), new LoginTokenRepository(fixture),
            new GameplayRepository(fixture));
        Assert.Equal(uid, await reopenedAccounts.LoginAsync("alice"u8.ToArray(), "password"u8.ToArray()));
        var sent = new List<(PacketType Type, byte[] Body)>();
        var session = new GumonjiSession(ServerKind.Game, reopenedAccounts, new(), (type, body, _) =>
        {
            sent.Add((type, body));
            return Task.CompletedTask;
        }) { UserId = uid, State = SessionState.WaitCharacterCheck };
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);

        Assert.True(await dispatcher.DispatchAsync(ServerKind.Game, PacketType.CharacterCheckExistRequest,
            ReadOnlyMemory<byte>.Empty, session));
        Assert.Equal(SessionState.WaitCharacterLoad, session.State);
        var found = Assert.Single(sent);
        Assert.Equal(PacketType.CharacterCheckExistResponse, found.Type);
        Assert.Equal("0000000100", Convert.ToHexString(found.Body).ToLowerInvariant());

        sent.Clear();
        Assert.True(await dispatcher.DispatchAsync(ServerKind.Game, PacketType.CharacterLoadRequest,
            ReadOnlyMemory<byte>.Empty, session));
        Assert.Equal(SessionState.CharacterCreated, session.State);
        Assert.Equal((uint)saved!.Id, session.CharacterId);
        var assigned = Assert.Single(sent);
        Assert.Equal(PacketType.CharacterAssignResponse, assigned.Type);
        var reader = new PacketReader(assigned.Body);
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal((uint)saved.Id, reader.ReadUInt32());
        reader.ExpectEnd();
        Assert.Equal("Alice"u8.ToArray(), (await reopenedAccounts.GetCharacterAsync(uid))!.Name);
    }

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
        Assert.Equal(1, item.Subtype);
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
        Assert.Equal(4, (await fixture.Repository.GetPlantAsync(1, 1000))!.Stage);
        Assert.Equal(16, (await fixture.Repository.GetInventoryAsync(uid)).Count);
    }

    [Fact]
    public async Task PlantingBambooSeedConsumesItAndRestoresYoungPlantOnReconnect()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        var sent = new List<(PacketType Type, byte[] Body)>();
        var session = await fixture.Session(uid, sent);
        session.PositionX = 71000;
        session.PositionY = 69000;
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);
        var harvested = await fixture.Repository.HarvestAsync(uid, 1, 1004, session.PositionX, session.PositionY);
        var seed = Assert.IsType<InventoryItem>(harvested.Item);
        Assert.Equal(5, seed.Subtype);
        var use = new PacketWriter();
        use.Write((uint)seed.Slot);
        use.Write(0u);
        use.Write((ushort)69);
        use.Write((ushort)68);
        use.Write(0u);

        Assert.True(await dispatcher.DispatchAsync(ServerKind.Game, PacketType.ItemUseRequest, use.ToBytes(), session));
        Assert.Equal(new[] { PacketType.ItemUseResponse, PacketType.InventorySlotResponse,
            PacketType.PlantPlaceResponse }, sent.Select(p => p.Type));
        var result = new PacketReader(sent[0].Body);
        Assert.Equal(0u, result.ReadUInt32());
        Assert.Equal(0u, result.ReadUInt32());
        Assert.Equal((uint)seed.Slot, result.ReadUInt32());
        var inventory = new PacketReader(sent[1].Body);
        Assert.Equal((byte)seed.Slot, inventory.ReadByte());
        Assert.Equal(0u, inventory.ReadUInt32());
        Assert.Equal((ushort)0, inventory.ReadUInt16());
        Assert.Empty(await fixture.Repository.GetInventoryAsync(uid));
        var planted = Assert.Single(await fixture.Repository.GetPlantsInChunkAsync(1, 2, 2),
            p => p.Id >= PlantWorld.PlantedIdBase);
        Assert.Equal((69, 68, 5, 8, 200, 0),
            (planted.X, planted.Y, planted.Subtype, planted.Color, planted.Fertility, planted.Stage));

        sent.Clear();
        var reconnect = await fixture.Session(uid, sent);
        var page = new PacketWriter();
        page.Write(2u);
        page.Write(2u);
        await dispatcher.DispatchAsync(ServerKind.Game, PacketType.GetPageDataRequest, page.ToBytes(), reconnect);
        Assert.Contains(sent, p => p.Type == PacketType.PlantPlaceResponse &&
            new PacketReader(p.Body).ReadUInt32() == (uint)planted.Id);
    }

    [Fact]
    public async Task PlantingRejectsOccupiedOrDistantTilesWithoutConsumingSeed()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        var harvested = await fixture.Repository.HarvestAsync(uid, 1, 1000, 70000, 69000);
        var seed = Assert.IsType<InventoryItem>(harvested.Item);
        Assert.NotNull((await fixture.Repository.PlantSeedAsync(uid, 1, (uint)seed.Slot,
            70, 69, 70000, 69000)).Error); // existing tree
        Assert.NotNull((await fixture.Repository.PlantSeedAsync(uid, 1, (uint)seed.Slot,
            98, 64, 70000, 69000)).Error); // too far away
        Assert.NotNull((await fixture.Repository.PlantSeedAsync(uid, 1, (uint)seed.Slot,
            98, 64, 98000, 64000)).Error); // water
        Assert.Single(await fixture.Repository.GetInventoryAsync(uid));
        Assert.DoesNotContain(await fixture.Repository.GetPlantsInChunkAsync(1, 2, 2),
            p => p.Id >= PlantWorld.PlantedIdBase);
    }

    [Theory]
    [InlineData(1000u, 1, 70, 69, 70, 70)]
    [InlineData(1001u, 2, 72, 69, 72, 70)]
    [InlineData(1002u, 3, 75, 75, 75, 76)]
    [InlineData(1003u, 4, 75, 74, 76, 74)]
    [InlineData(1004u, 5, 71, 69, 71, 70)]
    public async Task EachTreeYieldsItsMatchingPlantableSeed(
        uint treeId, int subtype, ushort treeX, ushort treeY, ushort plantX, ushort plantY)
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        var harvested = await fixture.Repository.HarvestAsync(
            uid, 1, treeId, (uint)treeX * 1000, (uint)treeY * 1000);
        var seed = Assert.IsType<InventoryItem>(harvested.Item);
        Assert.Equal((TreeSeeds.ItemType, subtype, TreeSeeds.Green),
            (seed.ItemType, seed.Subtype, seed.Color));
        var sent = new List<(PacketType Type, byte[] Body)>();
        var session = await fixture.Session(uid, sent);
        session.PositionX = (uint)treeX * 1000;
        session.PositionY = (uint)treeY * 1000;
        var use = new PacketWriter();
        use.Write((uint)seed.Slot);
        use.Write(0u);
        use.Write(plantX);
        use.Write(plantY);
        use.Write(0u);
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);
        Assert.True(await dispatcher.DispatchAsync(ServerKind.Game, PacketType.ItemUseRequest,
            use.ToBytes(), session));
        Assert.Equal(new[] { PacketType.ItemUseResponse, PacketType.InventorySlotResponse,
            PacketType.PlantPlaceResponse }, sent.Select(p => p.Type));
        var planted = Assert.Single(await fixture.Repository.GetPlantsInChunkAsync(1, 2, 2),
            p => p.Id >= PlantWorld.PlantedIdBase);
        Assert.Equal(subtype, planted.Subtype);
        Assert.Empty(await fixture.Repository.GetInventoryAsync(uid));
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
        Assert.Equal(20, await db.Plants.CountAsync());
        Assert.Equal(4, (await db.Plants.SingleAsync(p => p.ZoneId == 1 && p.Id == 1000)).Stage);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task UnifiedPlantsMigrationPreservesHarvestedAndPlayerPlantedTrees()
    {
        using var fixture = new DatabaseFixture(migrate: false);
        await using var db = fixture.CreateDbContext();
        await db.GetService<IMigrator>().MigrateAsync("20260930040016_PlantedSeeds");
        var uid = await fixture.CreatePlayer();
        var character = await fixture.Accounts.GetCharacterAsync(uid);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO PlantStates (ZoneId, PlantId, Fertility, Stage) VALUES (1, 1000, 35800, 0);
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO PlantedSeeds (Id, ZoneId, CharacterId, X, Y, Subtype, Color, Fertility, Stage)
            VALUES (7, 1, {character!.Id}, 69, 68, 5, 8, 200, 2);
            """);

        await db.Database.MigrateAsync();

        var grove = await db.Plants.SingleAsync(p => p.ZoneId == 1 && p.Id == 1000);
        Assert.Equal((3, 1, 70, 69, 35800, 0),
            (grove.Type, grove.Subtype, grove.X, grove.Y, grove.Fertility, grove.Stage));
        var planted = await db.Plants.SingleAsync(p => p.ZoneId == 1 && p.Id == 100007);
        Assert.Equal((character.Id, 3, 5, 8, 69, 68, 200, 2),
            (planted.CharacterId, planted.Type, planted.Subtype, planted.Color,
                planted.X, planted.Y, planted.Fertility, planted.Stage));
        Assert.Equal(21, await db.Plants.CountAsync());
    }

    [Fact]
    public async Task GrovePlacementAndHarvestFollowDatabaseRows()
    {
        using var fixture = new DatabaseFixture();
        var uid = await fixture.CreatePlayer();
        await using (var db = fixture.CreateDbContext())
        {
            var moved = await db.Plants.SingleAsync(p => p.ZoneId == 1 && p.Id == 1000);
            moved.X = 80;
            moved.Y = 80;
            db.Plants.Remove(await db.Plants.SingleAsync(p => p.ZoneId == 1 && p.Id == 1001));
            db.Plants.Add(new Plant
            {
                ZoneId = 1, Id = 1100, Type = 3, Subtype = 5, Color = 8,
                X = 81, Y = 80, Fertility = 8000, Stage = 4,
            });
            await db.SaveChangesAsync();
        }

        var chunk = await fixture.Repository.GetPlantsInChunkAsync(1, 2, 2);
        Assert.Contains(chunk, p => p.Id == 1000 && p.X == 80 && p.Y == 80);
        Assert.Contains(chunk, p => p.Id == 1100);
        Assert.DoesNotContain(chunk, p => p.Id == 1001);
        Assert.NotNull((await fixture.Repository.HarvestAsync(uid, 1, 1000, 70000, 69000)).Error);
        Assert.NotNull((await fixture.Repository.HarvestAsync(uid, 1, 1001, 72000, 69000)).Error);
        Assert.NotNull((await fixture.Repository.HarvestAsync(uid, 1, 1100, 81000, 80000)).Item);
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

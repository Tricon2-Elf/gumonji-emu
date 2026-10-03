using gumonji.Common;
using gumonji.Common.DAL;
using gumonji.Common.DAL.Repositories;
using gumonji.Network;
using gumonji.Network.Packets.Backd;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace gumonji.Common.Tests;

public sealed class BackdDispatcherTests
{
    [Fact]
    public void EveryBackdRequestHasAPacketAndHandler()
    {
        var factory = new TestContextFactory(new DbContextOptionsBuilder<MainContext>()
            .UseSqlite("Data Source=:memory:").Options);
        var dispatcher = CreateDispatcher(factory);
        var incoming = typeof(PacketType).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.GetCustomAttribute<PacketMetadata>() is
                { Server: ServerKind.Backd, Direction: PacketDirection.ClientToServer })
            .ToArray();
        Assert.Equal(incoming.Length, dispatcher.GetHandledPacketTypes(ServerKind.Backd).Count);
        foreach (var field in incoming)
        {
            Assert.Contains((PacketType)field.GetValue(null)!, dispatcher.GetHandledPacketTypes(ServerKind.Backd));
            var packetType = typeof(PacketType).Assembly.GetType(
                $"gumonji.Network.Packets.Backd.{field.Name[5..]}");
            Assert.NotNull(packetType);
            Assert.Contains(packetType.GetInterfaces(), i => i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IIncomingPacket<>));
        }
    }

    [Fact]
    public void SharedOpcodesResolveNamesWithinTheirServer()
    {
        Assert.Equal("BackdStatusRequest", PacketTypeInfo.Name(ServerKind.Backd, PacketType.BackdStatusRequest));
        Assert.Equal("HeartbeatRequest", PacketTypeInfo.Name(ServerKind.Femsg, PacketType.HeartbeatRequest));
        Assert.Equal("BackdStatusReply", PacketTypeInfo.Name(ServerKind.Backd, PacketType.BackdStatusReply));
        Assert.Equal("HeartbeatReply", PacketTypeInfo.Name(ServerKind.Femsg, PacketType.HeartbeatReply));
    }

    [Fact]
    public async Task LoginStatusLocksSaveLoadAndExistence()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db");
        var dbOptions = new DbContextOptionsBuilder<MainContext>()
            .UseSqlite($"Data Source={path}").Options;
        var factory = new TestContextFactory(dbOptions);
        await using (var db = await factory.CreateDbContextAsync())
            await db.Database.MigrateAsync();
        var state = new BackdState(factory);
        var dispatcher = CreateDispatcher(factory, new EmuOptions { BackdPassword = "secret" }, state);
        var sent = new List<(PacketType Id, byte[] Body)>();
        Task Send(PacketType id, byte[] body, CancellationToken _) { sent.Add((id, body)); return Task.CompletedTask; }
        var first = new BackdSession(Send);
        var second = new BackdSession(Send);

        var login = new PacketWriter();
        login.WriteCompactBytes("1"u8);
        login.WriteCompactBytes("secret"u8);
        login.Write((ushort)23432);
        login.Write(0u);
        await dispatcher.DispatchAsync(PacketType.BackdLoginRequest, login.ToBytes(), first);
        Assert.Equal(PacketType.BackdLoginReply, sent[^1].Id);
        var reply = new PacketReader(sent[^1].Body);
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Empty(reply.ReadCompactBytes());
        Assert.Equal(1u, reply.ReadUInt32());
        reply.ExpectEnd();

        await dispatcher.DispatchAsync(PacketType.BackdLoginRequest, login.ToBytes(), second);
        var status = new PacketWriter();
        foreach (var value in new uint[] { 10001, 184022225, 0, 0 }) status.Write(value);
        await dispatcher.DispatchAsync(PacketType.BackdStatusRequest, status.ToBytes(), first);
        Assert.Equal(PacketType.BackdStatusReply, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(10001u, reply.ReadUInt32());
        Assert.Equal(184022225u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        reply.ExpectEnd();

        var lockMessage = new PacketWriter();
        lockMessage.Write(77u); // msgid
        lockMessage.Write(42u); // uid
        await dispatcher.DispatchAsync(PacketType.BackdGetLockRequest, lockMessage.ToBytes(), first);
        Assert.Equal(PacketType.BackdGetLockReply, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal(42u, reply.ReadUInt32());

        await dispatcher.DispatchAsync(PacketType.BackdGetLockRequest, lockMessage.ToBytes(), second);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(unchecked((uint)-36), reply.ReadUInt32());
        Assert.Equal(42u, reply.ReadUInt32());

        var blob = new byte[] { 1, 2, 3, 4, 5 };
        var save = new PacketWriter();
        save.Write(77u);
        save.Write(42u);
        save.WriteCompactBytes(blob);
        save.Write((uint)'I');
        await dispatcher.DispatchAsync(PacketType.BackdSaveCharacterRequest, save.ToBytes(), first);
        Assert.Equal(PacketType.BackdSaveCharacterReply, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal((uint)'I', reply.ReadUInt32());

        var load = new PacketWriter();
        load.Write(77u);
        load.Write(42u);
        load.WriteCompactCount(2);
        load.Write((uint)'L');
        load.Write(0u);
        await dispatcher.DispatchAsync(PacketType.BackdLoadCharacterRequest, load.ToBytes(), first);
        Assert.Equal(PacketType.BackdLoadCharacterReply, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(42u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal(blob, reply.ReadCompactBytes());
        Assert.Equal(2, reply.ReadCompactLength());
        Assert.Equal((uint)'L', reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        reply.ExpectEnd();

        await dispatcher.DispatchAsync(PacketType.BackdCharacterExistsRequest, lockMessage.ToBytes(), first);
        Assert.Equal(PacketType.BackdCharacterExistsReply, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());

        await dispatcher.DispatchAsync(PacketType.BackdPutLockRequest, lockMessage.ToBytes(), first);
        Assert.Equal(PacketType.BackdPutLockReply, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal(42u, reply.ReadUInt32());
        state.Disconnect(first);
        await dispatcher.DispatchAsync(PacketType.BackdGetLockRequest, lockMessage.ToBytes(), second);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal(42u, reply.ReadUInt32());
    }

    [Fact]
    public async Task MissingCharacterReturnsOriginalErrorCode()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MainContext>().UseSqlite($"Data Source={path}").Options;
        var factory = new TestContextFactory(options);
        await using (var db = await factory.CreateDbContextAsync()) await db.Database.MigrateAsync();
        var dispatcher = CreateDispatcher(factory);
        (PacketType Id, byte[] Body) response = default;
        Task Send(PacketType id, byte[] body, CancellationToken _) { response = (id, body); return Task.CompletedTask; }
        var session = new BackdSession(Send) { ZoneName = "1" };
        var request = new PacketWriter();
        request.Write(1u);
        request.Write(999u);
        request.WriteCompactCount(2);
        request.Write((uint)'L');
        request.Write(0u);
        await dispatcher.DispatchAsync(PacketType.BackdLoadCharacterRequest, request.ToBytes(), session);
        Assert.Equal(PacketType.BackdLoadCharacterReply, response.Id);
        var reader = new PacketReader(response.Body);
        Assert.Equal(1u, reader.ReadUInt32());
        Assert.Equal(999u, reader.ReadUInt32());
        Assert.Equal(unchecked((uint)-13), reader.ReadUInt32());
        Assert.Empty(reader.ReadCompactBytes());
        Assert.Equal(2, reader.ReadCompactLength());
        Assert.Equal((uint)'L', reader.ReadUInt32());
        Assert.Equal(0u, reader.ReadUInt32());
        reader.ExpectEnd();
    }

    [Fact]
    public async Task InvalidPasswordDoesNotAuthenticateOrUnlockBackend()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MainContext>().UseSqlite($"Data Source={path}").Options;
        var dispatcher = CreateDispatcher(new TestContextFactory(options),
            new EmuOptions { BackdPassword = "correct" });
        (PacketType Id, byte[] Body) response = default;
        Task Send(PacketType id, byte[] body, CancellationToken _) { response = (id, body); return Task.CompletedTask; }
        var session = new BackdSession(Send);
        var login = new PacketWriter();
        login.WriteCompactBytes("1"u8);
        login.WriteCompactBytes("wrong"u8);
        login.Write((ushort)23432);
        login.Write(0u);
        await dispatcher.DispatchAsync(PacketType.BackdLoginRequest, login.ToBytes(), session);
        Assert.Equal(PacketType.BackdLoginReply, response.Id);
        var reader = new PacketReader(response.Body);
        Assert.NotEqual(0u, reader.ReadUInt32());
        Assert.False(session.Authenticated);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            dispatcher.DispatchAsync(PacketType.BackdStatusRequest, new byte[16], session));
    }

    [Fact]
    public async Task DoorIdsPersistAndPassageQueryReturnsEightSixEntryArrays()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MainContext>().UseSqlite($"Data Source={path}").Options;
        var factory = new TestContextFactory(options);
        await using (var db = await factory.CreateDbContextAsync()) await db.Database.MigrateAsync();
        (PacketType Id, byte[] Body) response = default;
        Task Send(PacketType id, byte[] body, CancellationToken _) { response = (id, body); return Task.CompletedTask; }
        var session = new BackdSession(Send) { ZoneName = "1" };
        var dispatcher = CreateDispatcher(factory);
        var request = new PacketWriter();
        request.Write(3u);
        await dispatcher.DispatchAsync(PacketType.BackdAllocateDoorIdsRequest, request.ToBytes(), session);
        Assert.Equal(PacketType.BackdAllocateDoorIdsReply, response.Id);
        var reader = new PacketReader(response.Body);
        Assert.Equal(3, reader.ReadCompactLength());
        Assert.Equal(new uint[] { 1, 2, 3 }, new[] { reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32() });
        reader.ExpectEnd();

        dispatcher = CreateDispatcher(factory);
        request = new PacketWriter();
        request.Write(2u);
        await dispatcher.DispatchAsync(PacketType.BackdAllocateDoorIdsRequest, request.ToBytes(), session);
        reader = new PacketReader(response.Body);
        Assert.Equal(2, reader.ReadCompactLength());
        Assert.Equal(4u, reader.ReadUInt32());
        Assert.Equal(5u, reader.ReadUInt32());
        reader.ExpectEnd();

        request = new PacketWriter();
        request.Write(1u); // zone ID
        request.Write(100u); // zone width
        request.Write(100u); // zone height
        await dispatcher.DispatchAsync(PacketType.BackdPassageLinksRequest, request.ToBytes(), session);
        Assert.Equal(PacketType.BackdPassageLinksReply, response.Id);
        reader = new PacketReader(response.Body);
        Assert.Equal(1u, reader.ReadUInt32());
        for (var array = 0; array < 8; array++)
        {
            Assert.Equal(6, reader.ReadCompactLength());
            for (var index = 0; index < 6; index++)
                Assert.Equal(0u, reader.ReadUInt32());
        }
        reader.ExpectEnd();
    }

    [Fact]
    public async Task OneWayZoneNotificationsAcceptValidBodiesWithoutSendingReplies()
    {
        var options = new DbContextOptionsBuilder<MainContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db")}")
            .Options;
        var dispatcher = CreateDispatcher(new TestContextFactory(options));
        var responses = new List<PacketType>();
        Task Send(PacketType id, byte[] body, CancellationToken ct) { responses.Add(id); return Task.CompletedTask; }
        var session = new BackdSession(Send) { ZoneName = "1" };

        var online = new PacketWriter();
        online.Write(42u);
        await dispatcher.DispatchAsync(PacketType.BackdUserOnlineRequest, online.ToBytes(), session);

        var logout = new PacketWriter();
        logout.Write(42u);
        await dispatcher.DispatchAsync(PacketType.BackdUserOfflineRequest, logout.ToBytes(), session);

        var sellers = new PacketWriter();
        sellers.WriteCompactCount(2);
        sellers.Write(101u);
        sellers.Write(102u);
        await dispatcher.DispatchAsync(PacketType.BackdSellerIdsRequest, sellers.ToBytes(), session);

        var audit = new PacketWriter();
        audit.Write(200u);
        audit.Write(42u);
        audit.WriteCompactBytes("Tester"u8);
        audit.WriteCompactBytes("Disconnected"u8);
        await dispatcher.DispatchAsync(PacketType.BackdAuditRequest, audit.ToBytes(), session);
        Assert.Empty(responses);

        var status = new PacketWriter();
        foreach (var value in new uint[] { 10001, 184022225, 0, 0 }) status.Write(value);
        await dispatcher.DispatchAsync(PacketType.BackdStatusRequest, status.ToBytes(), session);
        Assert.Equal(new[] { PacketType.BackdStatusReply }, responses);
    }

    [Fact]
    public async Task ZonePasswordCheckConsumesFrontendToken()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MainContext>().UseSqlite($"Data Source={path}").Options;
        var factory = new TestContextFactory(options);
        await using (var db = await factory.CreateDbContextAsync()) await db.Database.MigrateAsync();
        var uid = await new AccountRepository(factory).GetOrCreateAsync("tester"u8.ToArray(), "password"u8.ToArray());
        var token = await new LoginTokenRepository(factory).IssueAsync(uid, null);
        var dispatcher = CreateDispatcher(factory);
        (PacketType Id, byte[] Body) response = default;
        Task Send(PacketType id, byte[] body, CancellationToken ct) { response = (id, body); return Task.CompletedTask; }
        var session = new BackdSession(Send) { ZoneName = "1" };
        var request = new PacketWriter();
        request.Write(77u); // msgid
        request.Write(uid);
        request.WriteCompactBytes(token);

        await dispatcher.DispatchAsync(PacketType.BackdCheckPasswordRequest, request.ToBytes(), session);
        Assert.Equal(PacketType.BackdCheckPasswordReply, response.Id);
        var reader = new PacketReader(response.Body);
        Assert.Equal(77u, reader.ReadUInt32());
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal(uid, reader.ReadUInt32());
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal(1u, reader.ReadUInt32());
        Assert.Equal("tester"u8.ToArray(), reader.ReadCompactBytes());
        Assert.Empty(reader.ReadCompactBytes());
        Assert.Equal((ushort)1, reader.ReadUInt16()); // land-edit entitlement
        reader.ExpectEnd();

        await dispatcher.DispatchAsync(PacketType.BackdCheckPasswordRequest, request.ToBytes(), session);
        reader = new PacketReader(response.Body);
        Assert.Equal(77u, reader.ReadUInt32());
        Assert.Equal(unchecked((uint)-3), reader.ReadUInt32());
        Assert.Equal(uid, reader.ReadUInt32());
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Empty(reader.ReadCompactBytes());
        Assert.Empty(reader.ReadCompactBytes());
        Assert.Equal((ushort)0, reader.ReadUInt16());
        reader.ExpectEnd();
    }

    [Fact]
    public async Task VendorZoneQueryReturnsEmptyListWithoutDisconnecting()
    {
        var dispatcher = CreateDispatcher(
            new TestContextFactory(new DbContextOptionsBuilder<MainContext>()
                .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db")}")
                .Options));
        (PacketType Id, byte[] Body) response = default;
        Task Send(PacketType id, byte[] body, CancellationToken ct) { response = (id, body); return Task.CompletedTask; }
        var session = new BackdSession(Send) { ZoneName = "1" };
        var request = new PacketWriter();
        request.Write(91u); // msgid
        request.Write(42u); // player uid
        await dispatcher.DispatchAsync(PacketType.BackdVendorZonesRequest, request.ToBytes(), session);
        Assert.Equal(PacketType.BackdVendorZonesReply, response.Id);
        var reader = new PacketReader(response.Body);
        Assert.Equal(91u, reader.ReadUInt32());
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal(0, reader.ReadCompactLength());
        reader.ExpectEnd();
    }

    [Fact]
    public async Task HistoryLoadHasExactlyTwentyFourValuesAndSavePersistsThem()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MainContext>().UseSqlite($"Data Source={path}").Options;
        var factory = new TestContextFactory(options);
        await using (var db = await factory.CreateDbContextAsync()) await db.Database.MigrateAsync();
        var dispatcher = CreateDispatcher(factory);
        (PacketType Id, byte[] Body) response = default;
        Task Send(PacketType id, byte[] body, CancellationToken ct) { response = (id, body); return Task.CompletedTask; }
        var session = new BackdSession(Send) { ZoneName = "1" };
        var load = new PacketWriter();
        load.Write(91u); // msgid
        load.Write(42u); // uid
        await dispatcher.DispatchAsync(PacketType.BackdLoadHistoryRequest, load.ToBytes(), session);
        Assert.Equal(PacketType.BackdLoadHistoryReply, response.Id);
        var reader = new PacketReader(response.Body);
        Assert.Equal(91u, reader.ReadUInt32());
        Assert.Equal(42u, reader.ReadUInt32());
        Assert.Equal(24, reader.ReadCompactLength());
        Assert.Equal(42u, reader.ReadUInt32());
        for (var i = 1; i < 24; i++) Assert.Equal(0u, reader.ReadUInt32());
        reader.ExpectEnd();

        var saved = new uint[24];
        saved[0] = 42;
        saved[1] = 3600; // played seconds
        saved[6] = 150; // movement history
        var save = new PacketWriter();
        foreach (var value in saved) save.Write(value);
        await dispatcher.DispatchAsync(PacketType.BackdSaveHistoryRequest, save.ToBytes(), session);
        Assert.Equal(PacketType.BackdSaveHistoryReply, response.Id);
        reader = new PacketReader(response.Body);
        Assert.Equal(0u, reader.ReadUInt32());
        reader.ExpectEnd();

        dispatcher = CreateDispatcher(factory);
        await dispatcher.DispatchAsync(PacketType.BackdLoadHistoryRequest, load.ToBytes(), session);
        reader = new PacketReader(response.Body);
        Assert.Equal(91u, reader.ReadUInt32());
        Assert.Equal(42u, reader.ReadUInt32());
        Assert.Equal(24, reader.ReadCompactLength());
        foreach (var value in saved) Assert.Equal(value, reader.ReadUInt32());
        reader.ExpectEnd();
    }

    private static PacketDispatcher CreateDispatcher(IDbContextFactory<MainContext> factory,
        EmuOptions? options = null, BackdState? state = null) =>
        TestPacketDispatcher.Create(factory, options, state);

    private sealed class TestContextFactory(DbContextOptions<MainContext> options)
        : IDbContextFactory<MainContext>
    {
        public MainContext CreateDbContext() => new(options);
        public Task<MainContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(new MainContext(options));
    }
}

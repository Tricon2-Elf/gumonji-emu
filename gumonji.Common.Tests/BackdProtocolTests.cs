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

public sealed class BackdProtocolTests
{
    [Fact]
    public void EveryBackdRequestHasAPacketAndHandler()
    {
        var factory = new TestContextFactory(new DbContextOptionsBuilder<MainContext>()
            .UseSqlite("Data Source=:memory:").Options);
        var protocol = CreateProtocol(factory);
        var incoming = typeof(PacketType).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.GetCustomAttribute<PacketMetadata>() is
                { Server: ServerKind.Backd, Direction: PacketDirection.ClientToServer })
            .ToArray();
        Assert.Equal(incoming.Length, protocol.HandledPacketTypes.Count);
        foreach (var field in incoming)
        {
            Assert.Contains((PacketType)field.GetValue(null)!, protocol.HandledPacketTypes);
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
        var protocol = CreateProtocol(factory, new EmuOptions { BackdPassword = "secret" });
        var first = new BackdSession();
        var second = new BackdSession();
        var sent = new List<(PacketType Id, byte[] Body)>();
        Task Send(PacketType id, byte[] body, CancellationToken _) { sent.Add((id, body)); return Task.CompletedTask; }

        var login = new PacketWriter();
        login.WriteCompactBytes("1"u8);
        login.WriteCompactBytes("secret"u8);
        login.Write((ushort)23432);
        login.Write(0u);
        await protocol.HandleAsync(first, PacketType.BackdLoginRequest, login.ToBytes(), Send);
        Assert.Equal(PacketType.BackdLoginReply, sent[^1].Id);
        var reply = new PacketReader(sent[^1].Body);
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Empty(reply.ReadCompactBytes());
        Assert.Equal(1u, reply.ReadUInt32());
        reply.ExpectEnd();

        await protocol.HandleAsync(second, PacketType.BackdLoginRequest, login.ToBytes(), Send);
        var status = new PacketWriter();
        foreach (var value in new uint[] { 10001, 184022225, 0, 0 }) status.Write(value);
        await protocol.HandleAsync(first, PacketType.BackdStatusRequest, status.ToBytes(), Send);
        Assert.Equal(PacketType.BackdStatusReply, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(10001u, reply.ReadUInt32());
        Assert.Equal(184022225u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        reply.ExpectEnd();

        var lockMessage = new PacketWriter();
        lockMessage.Write(77u); // msgid
        lockMessage.Write(42u); // uid
        await protocol.HandleAsync(first, PacketType.BackdGetLockRequest, lockMessage.ToBytes(), Send);
        Assert.Equal(PacketType.BackdGetLockReply, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal(42u, reply.ReadUInt32());

        await protocol.HandleAsync(second, PacketType.BackdGetLockRequest, lockMessage.ToBytes(), Send);
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
        await protocol.HandleAsync(first, PacketType.BackdSaveCharacterRequest, save.ToBytes(), Send);
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
        await protocol.HandleAsync(first, PacketType.BackdLoadCharacterRequest, load.ToBytes(), Send);
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

        await protocol.HandleAsync(first, PacketType.BackdCharacterExistsRequest, lockMessage.ToBytes(), Send);
        Assert.Equal(PacketType.BackdCharacterExistsReply, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());

        await protocol.HandleAsync(first, PacketType.BackdPutLockRequest, lockMessage.ToBytes(), Send);
        Assert.Equal(PacketType.BackdPutLockReply, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal(42u, reply.ReadUInt32());
        protocol.Disconnect(first);
        await protocol.HandleAsync(second, PacketType.BackdGetLockRequest, lockMessage.ToBytes(), Send);
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
        var protocol = CreateProtocol(factory);
        var session = new BackdSession { ZoneName = "1" };
        (PacketType Id, byte[] Body) response = default;
        Task Send(PacketType id, byte[] body, CancellationToken _) { response = (id, body); return Task.CompletedTask; }
        var request = new PacketWriter();
        request.Write(1u);
        request.Write(999u);
        request.WriteCompactCount(2);
        request.Write((uint)'L');
        request.Write(0u);
        await protocol.HandleAsync(session, PacketType.BackdLoadCharacterRequest, request.ToBytes(), Send);
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
        var protocol = CreateProtocol(new TestContextFactory(options),
            new EmuOptions { BackdPassword = "correct" });
        var session = new BackdSession();
        (PacketType Id, byte[] Body) response = default;
        Task Send(PacketType id, byte[] body, CancellationToken _) { response = (id, body); return Task.CompletedTask; }
        var login = new PacketWriter();
        login.WriteCompactBytes("1"u8);
        login.WriteCompactBytes("wrong"u8);
        login.Write((ushort)23432);
        login.Write(0u);
        await protocol.HandleAsync(session, PacketType.BackdLoginRequest, login.ToBytes(), Send);
        Assert.Equal(PacketType.BackdLoginReply, response.Id);
        var reader = new PacketReader(response.Body);
        Assert.NotEqual(0u, reader.ReadUInt32());
        Assert.False(session.Authenticated);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            protocol.HandleAsync(session, PacketType.BackdStatusRequest, new byte[16], Send));
    }

    [Fact]
    public async Task DoorIdsPersistAndPassageQueryReturnsEightSixEntryArrays()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MainContext>().UseSqlite($"Data Source={path}").Options;
        var factory = new TestContextFactory(options);
        await using (var db = await factory.CreateDbContextAsync()) await db.Database.MigrateAsync();
        var session = new BackdSession { ZoneName = "1" };
        (PacketType Id, byte[] Body) response = default;
        Task Send(PacketType id, byte[] body, CancellationToken _) { response = (id, body); return Task.CompletedTask; }
        var protocol = CreateProtocol(factory);
        var request = new PacketWriter();
        request.Write(3u);
        await protocol.HandleAsync(session, PacketType.BackdAllocateDoorIdsRequest, request.ToBytes(), Send);
        Assert.Equal(PacketType.BackdAllocateDoorIdsReply, response.Id);
        var reader = new PacketReader(response.Body);
        Assert.Equal(3, reader.ReadCompactLength());
        Assert.Equal(new uint[] { 1, 2, 3 }, new[] { reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32() });
        reader.ExpectEnd();

        protocol = CreateProtocol(factory);
        request = new PacketWriter();
        request.Write(2u);
        await protocol.HandleAsync(session, PacketType.BackdAllocateDoorIdsRequest, request.ToBytes(), Send);
        reader = new PacketReader(response.Body);
        Assert.Equal(2, reader.ReadCompactLength());
        Assert.Equal(4u, reader.ReadUInt32());
        Assert.Equal(5u, reader.ReadUInt32());
        reader.ExpectEnd();

        request = new PacketWriter();
        request.Write(1u); // zone ID
        request.Write(100u); // zone width
        request.Write(100u); // zone height
        await protocol.HandleAsync(session, PacketType.BackdPassageLinksRequest, request.ToBytes(), Send);
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
        var protocol = CreateProtocol(new TestContextFactory(options));
        var session = new BackdSession { ZoneName = "1" };
        var responses = new List<PacketType>();
        Task Send(PacketType id, byte[] body, CancellationToken ct) { responses.Add(id); return Task.CompletedTask; }

        var online = new PacketWriter();
        online.Write(42u);
        await protocol.HandleAsync(session, PacketType.BackdUserOnlineRequest, online.ToBytes(), Send);

        var logout = new PacketWriter();
        logout.Write(42u);
        await protocol.HandleAsync(session, PacketType.BackdUserOfflineRequest, logout.ToBytes(), Send);

        var sellers = new PacketWriter();
        sellers.WriteCompactCount(2);
        sellers.Write(101u);
        sellers.Write(102u);
        await protocol.HandleAsync(session, PacketType.BackdSellerIdsRequest, sellers.ToBytes(), Send);

        var audit = new PacketWriter();
        audit.Write(200u);
        audit.Write(42u);
        audit.WriteCompactBytes("Tester"u8);
        audit.WriteCompactBytes("Disconnected"u8);
        await protocol.HandleAsync(session, PacketType.BackdAuditRequest, audit.ToBytes(), Send);
        Assert.Empty(responses);

        var status = new PacketWriter();
        foreach (var value in new uint[] { 10001, 184022225, 0, 0 }) status.Write(value);
        await protocol.HandleAsync(session, PacketType.BackdStatusRequest, status.ToBytes(), Send);
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
        var protocol = CreateProtocol(factory);
        var session = new BackdSession { ZoneName = "1" };
        (PacketType Id, byte[] Body) response = default;
        Task Send(PacketType id, byte[] body, CancellationToken ct) { response = (id, body); return Task.CompletedTask; }
        var request = new PacketWriter();
        request.Write(77u); // msgid
        request.Write(uid);
        request.WriteCompactBytes(token);

        await protocol.HandleAsync(session, PacketType.BackdCheckPasswordRequest, request.ToBytes(), Send);
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

        await protocol.HandleAsync(session, PacketType.BackdCheckPasswordRequest, request.ToBytes(), Send);
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
        var protocol = CreateProtocol(
            new TestContextFactory(new DbContextOptionsBuilder<MainContext>()
                .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db")}")
                .Options));
        var session = new BackdSession { ZoneName = "1" };
        (PacketType Id, byte[] Body) response = default;
        Task Send(PacketType id, byte[] body, CancellationToken ct) { response = (id, body); return Task.CompletedTask; }
        var request = new PacketWriter();
        request.Write(91u); // msgid
        request.Write(42u); // player uid
        await protocol.HandleAsync(session, PacketType.BackdVendorZonesRequest, request.ToBytes(), Send);
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
        var protocol = CreateProtocol(factory);
        var session = new BackdSession { ZoneName = "1" };
        (PacketType Id, byte[] Body) response = default;
        Task Send(PacketType id, byte[] body, CancellationToken ct) { response = (id, body); return Task.CompletedTask; }
        var load = new PacketWriter();
        load.Write(91u); // msgid
        load.Write(42u); // uid
        await protocol.HandleAsync(session, PacketType.BackdLoadHistoryRequest, load.ToBytes(), Send);
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
        await protocol.HandleAsync(session, PacketType.BackdSaveHistoryRequest, save.ToBytes(), Send);
        Assert.Equal(PacketType.BackdSaveHistoryReply, response.Id);
        reader = new PacketReader(response.Body);
        Assert.Equal(0u, reader.ReadUInt32());
        reader.ExpectEnd();

        protocol = CreateProtocol(factory);
        await protocol.HandleAsync(session, PacketType.BackdLoadHistoryRequest, load.ToBytes(), Send);
        reader = new PacketReader(response.Body);
        Assert.Equal(91u, reader.ReadUInt32());
        Assert.Equal(42u, reader.ReadUInt32());
        Assert.Equal(24, reader.ReadCompactLength());
        foreach (var value in saved) Assert.Equal(value, reader.ReadUInt32());
        reader.ExpectEnd();
    }

    private static BackdProtocol CreateProtocol(IDbContextFactory<MainContext> factory,
        EmuOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(options ?? new EmuOptions());
        services.AddSingleton(factory);
        services.AddLogging();
        services.AddBackdProtocol();
        return services.BuildServiceProvider().GetRequiredService<BackdProtocol>();
    }

    private sealed class TestContextFactory(DbContextOptions<MainContext> options)
        : IDbContextFactory<MainContext>
    {
        public MainContext CreateDbContext() => new(options);
        public Task<MainContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(new MainContext(options));
    }
}

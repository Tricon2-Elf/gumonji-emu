using gumonji.Common;
using gumonji.Common.DAL;
using gumonji.Common.DAL.Repositories;
using gumonji.Network;
using gumonji.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace gumonji.Common.Tests;

public sealed class BackdProtocolTests
{
    [Fact]
    public async Task LoginStatusLocksSaveLoadAndExistence()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db");
        var dbOptions = new DbContextOptionsBuilder<MainContext>()
            .UseSqlite($"Data Source={path}").Options;
        var factory = new TestContextFactory(dbOptions);
        await using (var db = await factory.CreateDbContextAsync())
            await db.Database.MigrateAsync();
        var protocol = new BackdProtocol(new EmuOptions { BackdPassword = "secret" },
            factory, NullLogger<BackdProtocol>.Instance);
        var first = new BackdSession();
        var second = new BackdSession();
        var sent = new List<(ushort Id, byte[] Body)>();
        Task Send(ushort id, byte[] body, CancellationToken _) { sent.Add((id, body)); return Task.CompletedTask; }

        var login = new PacketWriter();
        login.WriteCompactBytes("1"u8);
        login.WriteCompactBytes("secret"u8);
        login.Write((ushort)23432);
        login.Write(0u);
        await protocol.HandleAsync(first, 1, login.ToBytes(), Send);
        Assert.Equal((ushort)2, sent[^1].Id);
        var reply = new PacketReader(sent[^1].Body);
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Empty(reply.ReadCompactBytes());
        Assert.Equal(1u, reply.ReadUInt32());
        reply.ExpectEnd();

        await protocol.HandleAsync(second, 1, login.ToBytes(), Send);
        var status = new PacketWriter();
        foreach (var value in new uint[] { 10001, 184022225, 0, 0 }) status.Write(value);
        await protocol.HandleAsync(first, 5, status.ToBytes(), Send);
        Assert.Equal((ushort)6, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(10001u, reply.ReadUInt32());
        Assert.Equal(184022225u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        reply.ExpectEnd();

        var lockMessage = new PacketWriter();
        lockMessage.Write(77u); // msgid
        lockMessage.Write(42u); // uid
        await protocol.HandleAsync(first, 201, lockMessage.ToBytes(), Send);
        Assert.Equal((ushort)202, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal(42u, reply.ReadUInt32());

        await protocol.HandleAsync(second, 201, lockMessage.ToBytes(), Send);
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
        await protocol.HandleAsync(first, 501, save.ToBytes(), Send);
        Assert.Equal((ushort)502, sent[^1].Id);
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
        await protocol.HandleAsync(first, 503, load.ToBytes(), Send);
        Assert.Equal((ushort)504, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(42u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal(blob, reply.ReadCompactBytes());
        Assert.Equal(2, reply.ReadCompactLength());
        Assert.Equal((uint)'L', reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        reply.ExpectEnd();

        await protocol.HandleAsync(first, 507, lockMessage.ToBytes(), Send);
        Assert.Equal((ushort)508, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());

        await protocol.HandleAsync(first, 203, lockMessage.ToBytes(), Send);
        Assert.Equal((ushort)204, sent[^1].Id);
        reply = new PacketReader(sent[^1].Body);
        Assert.Equal(77u, reply.ReadUInt32());
        Assert.Equal(0u, reply.ReadUInt32());
        Assert.Equal(42u, reply.ReadUInt32());
        protocol.Disconnect(first);
        await protocol.HandleAsync(second, 201, lockMessage.ToBytes(), Send);
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
        var protocol = new BackdProtocol(new EmuOptions(), factory, NullLogger<BackdProtocol>.Instance);
        var session = new BackdSession { ZoneName = "1" };
        (ushort Id, byte[] Body) response = default;
        Task Send(ushort id, byte[] body, CancellationToken _) { response = (id, body); return Task.CompletedTask; }
        var request = new PacketWriter();
        request.Write(1u);
        request.Write(999u);
        request.WriteCompactCount(2);
        request.Write((uint)'L');
        request.Write(0u);
        await protocol.HandleAsync(session, 503, request.ToBytes(), Send);
        Assert.Equal((ushort)504, response.Id);
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
        var protocol = new BackdProtocol(new EmuOptions { BackdPassword = "correct" },
            new TestContextFactory(options), NullLogger<BackdProtocol>.Instance);
        var session = new BackdSession();
        (ushort Id, byte[] Body) response = default;
        Task Send(ushort id, byte[] body, CancellationToken _) { response = (id, body); return Task.CompletedTask; }
        var login = new PacketWriter();
        login.WriteCompactBytes("1"u8);
        login.WriteCompactBytes("wrong"u8);
        login.Write((ushort)23432);
        login.Write(0u);
        await protocol.HandleAsync(session, 1, login.ToBytes(), Send);
        Assert.Equal((ushort)2, response.Id);
        var reader = new PacketReader(response.Body);
        Assert.NotEqual(0u, reader.ReadUInt32());
        Assert.False(session.Authenticated);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            protocol.HandleAsync(session, 5, new byte[16], Send));
    }

    [Fact]
    public async Task DoorIdsPersistAndPassageQueryReturnsEightSixEntryArrays()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MainContext>().UseSqlite($"Data Source={path}").Options;
        var factory = new TestContextFactory(options);
        await using (var db = await factory.CreateDbContextAsync()) await db.Database.MigrateAsync();
        var session = new BackdSession { ZoneName = "1" };
        (ushort Id, byte[] Body) response = default;
        Task Send(ushort id, byte[] body, CancellationToken _) { response = (id, body); return Task.CompletedTask; }
        var protocol = new BackdProtocol(new EmuOptions(), factory, NullLogger<BackdProtocol>.Instance);
        var request = new PacketWriter();
        request.Write(3u);
        await protocol.HandleAsync(session, 1501, request.ToBytes(), Send);
        Assert.Equal((ushort)1502, response.Id);
        var reader = new PacketReader(response.Body);
        Assert.Equal(3, reader.ReadCompactLength());
        Assert.Equal(new uint[] { 1, 2, 3 }, new[] { reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32() });
        reader.ExpectEnd();

        protocol = new BackdProtocol(new EmuOptions(), factory, NullLogger<BackdProtocol>.Instance);
        request = new PacketWriter();
        request.Write(2u);
        await protocol.HandleAsync(session, 1501, request.ToBytes(), Send);
        reader = new PacketReader(response.Body);
        Assert.Equal(2, reader.ReadCompactLength());
        Assert.Equal(4u, reader.ReadUInt32());
        Assert.Equal(5u, reader.ReadUInt32());
        reader.ExpectEnd();

        request = new PacketWriter();
        request.Write(1u); // zone ID
        request.Write(100u); // zone width
        request.Write(100u); // zone height
        await protocol.HandleAsync(session, 2409, request.ToBytes(), Send);
        Assert.Equal((ushort)2410, response.Id);
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
        var protocol = new BackdProtocol(new EmuOptions(), new TestContextFactory(options),
            NullLogger<BackdProtocol>.Instance);
        var session = new BackdSession { ZoneName = "1" };
        var responses = new List<ushort>();
        Task Send(ushort id, byte[] body, CancellationToken ct) { responses.Add(id); return Task.CompletedTask; }

        var online = new PacketWriter();
        online.Write(42u);
        await protocol.HandleAsync(session, 1301, online.ToBytes(), Send);

        var logout = new PacketWriter();
        logout.Write(42u);
        await protocol.HandleAsync(session, 1302, logout.ToBytes(), Send);

        var sellers = new PacketWriter();
        sellers.WriteCompactCount(2);
        sellers.Write(101u);
        sellers.Write(102u);
        await protocol.HandleAsync(session, 1701, sellers.ToBytes(), Send);

        var audit = new PacketWriter();
        audit.Write(200u);
        audit.Write(42u);
        audit.WriteCompactBytes("Tester"u8);
        audit.WriteCompactBytes("Disconnected"u8);
        await protocol.HandleAsync(session, 2001, audit.ToBytes(), Send);
        Assert.Empty(responses);

        var status = new PacketWriter();
        foreach (var value in new uint[] { 10001, 184022225, 0, 0 }) status.Write(value);
        await protocol.HandleAsync(session, 5, status.ToBytes(), Send);
        Assert.Equal(new ushort[] { 6 }, responses);
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
        var protocol = new BackdProtocol(new EmuOptions(), factory, NullLogger<BackdProtocol>.Instance);
        var session = new BackdSession { ZoneName = "1" };
        (ushort Id, byte[] Body) response = default;
        Task Send(ushort id, byte[] body, CancellationToken ct) { response = (id, body); return Task.CompletedTask; }
        var request = new PacketWriter();
        request.Write(77u); // msgid
        request.Write(uid);
        request.WriteCompactBytes(token);

        await protocol.HandleAsync(session, 107, request.ToBytes(), Send);
        Assert.Equal((ushort)108, response.Id);
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

        await protocol.HandleAsync(session, 107, request.ToBytes(), Send);
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
        var protocol = new BackdProtocol(new EmuOptions(),
            new TestContextFactory(new DbContextOptionsBuilder<MainContext>()
                .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), $"gumonji-backd-test-{Guid.NewGuid():N}.db")}")
                .Options), NullLogger<BackdProtocol>.Instance);
        var session = new BackdSession { ZoneName = "1" };
        (ushort Id, byte[] Body) response = default;
        Task Send(ushort id, byte[] body, CancellationToken ct) { response = (id, body); return Task.CompletedTask; }
        var request = new PacketWriter();
        request.Write(91u); // msgid
        request.Write(42u); // player uid
        await protocol.HandleAsync(session, 1702, request.ToBytes(), Send);
        Assert.Equal((ushort)1703, response.Id);
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
        var protocol = new BackdProtocol(new EmuOptions(), factory, NullLogger<BackdProtocol>.Instance);
        var session = new BackdSession { ZoneName = "1" };
        (ushort Id, byte[] Body) response = default;
        Task Send(ushort id, byte[] body, CancellationToken ct) { response = (id, body); return Task.CompletedTask; }
        var load = new PacketWriter();
        load.Write(91u); // msgid
        load.Write(42u); // uid
        await protocol.HandleAsync(session, 2111, load.ToBytes(), Send);
        Assert.Equal((ushort)2112, response.Id);
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
        await protocol.HandleAsync(session, 2101, save.ToBytes(), Send);
        Assert.Equal((ushort)2102, response.Id);
        reader = new PacketReader(response.Body);
        Assert.Equal(0u, reader.ReadUInt32());
        reader.ExpectEnd();

        protocol = new BackdProtocol(new EmuOptions(), factory, NullLogger<BackdProtocol>.Instance);
        await protocol.HandleAsync(session, 2111, load.ToBytes(), Send);
        reader = new PacketReader(response.Body);
        Assert.Equal(91u, reader.ReadUInt32());
        Assert.Equal(42u, reader.ReadUInt32());
        Assert.Equal(24, reader.ReadCompactLength());
        foreach (var value in saved) Assert.Equal(value, reader.ReadUInt32());
        reader.ExpectEnd();
    }

    private sealed class TestContextFactory(DbContextOptions<MainContext> options)
        : IDbContextFactory<MainContext>
    {
        public MainContext CreateDbContext() => new(options);
        public Task<MainContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(new MainContext(options));
    }
}

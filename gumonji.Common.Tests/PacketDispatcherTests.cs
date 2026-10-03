using gumonji.Network;
using gumonji.Network.Packets.Backd;
using Xunit;

namespace gumonji.Common.Tests;

public sealed class PacketDispatcherTests
{
    [Fact]
    public async Task SharedOpcodeDispatchesByServerAndPreservesBothReplyFormats()
    {
        using var database = new TestDatabaseFixture();
        var dispatcher = TestPacketDispatcher.Create(database);
        var frontendSent = new List<(PacketType Type, byte[] Body)>();
        var backdSent = new List<(PacketType Type, byte[] Body)>();
        var frontend = new GumonjiSession(ServerKind.Femsg, database.Accounts, database.Characters,
            database.LoginTokens, database.Gameplay, new(), (type, body, _) =>
            { frontendSent.Add((type, body)); return Task.CompletedTask; });
        var backd = new BackdSession((type, body, _) =>
            { backdSent.Add((type, body)); return Task.CompletedTask; }) { ZoneName = "1" };
        var payload = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();

        Assert.Equal((uint)PacketType.HeartbeatRequest, (uint)PacketType.BackdStatusRequest);
        Assert.True(await dispatcher.DispatchAsync(PacketType.HeartbeatRequest, payload, frontend));
        Assert.True(await dispatcher.DispatchAsync(PacketType.BackdStatusRequest, payload, backd));
        var heartbeat = Assert.Single(frontendSent);
        Assert.Equal(PacketType.HeartbeatReply, heartbeat.Type);
        Assert.Equal(payload[..12], heartbeat.Body);
        var response = Assert.Single(backdSent);
        Assert.Equal(PacketType.BackdStatusReply, response.Type);
        Assert.Equal(new StatusReply(10001, 184022225, 0).ToBytes(), response.Body);
    }

    [Fact]
    public async Task SessionPoliciesPreserveAuthenticationAndUnknownOpcodeBehavior()
    {
        using var database = new TestDatabaseFixture();
        var dispatcher = TestPacketDispatcher.Create(database);
        var backd = new BackdSession((_, _, _) => throw new Xunit.Sdk.XunitException("unexpected reply"));
        var auth = await Assert.ThrowsAsync<InvalidDataException>(() =>
            dispatcher.DispatchAsync(PacketType.BackdStatusRequest, ReadOnlyMemory<byte>.Empty, backd));
        Assert.Equal("backd message before frontend_login", auth.Message); // before malformed-body decoding
        var unknown = (PacketType)0xffffffff;
        var unsupported = await Assert.ThrowsAsync<InvalidDataException>(() =>
            dispatcher.DispatchAsync(unknown, ReadOnlyMemory<byte>.Empty, backd));
        Assert.Contains("unsupported backd opcode", unsupported.Message);
        var zone = new GumonjiSession(ServerKind.Zone, database.Accounts, database.Characters,
            database.LoginTokens, database.Gameplay, new(), (_, _, _) => Task.CompletedTask);
        Assert.False(await dispatcher.DispatchAsync(unknown, ReadOnlyMemory<byte>.Empty, zone));
        backd.ZoneName = "1";
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            dispatcher.DispatchAsync(PacketType.BackdStatusRequest, ReadOnlyMemory<byte>.Empty, backd));
    }

    [Fact]
    public async Task IncorrectServerOrSessionTypeCannotReachAHandler()
    {
        using var database = new TestDatabaseFixture();
        var dispatcher = TestPacketDispatcher.Create(database);
        var backd = new BackdSession((_, _, _) => throw new Xunit.Sdk.XunitException("unexpected reply"))
            { ZoneName = "1" };
        await Assert.ThrowsAsync<InvalidDataException>(() => dispatcher.DispatchAsync(ServerKind.Femsg,
            PacketType.HeartbeatRequest, new byte[16], backd));
        var wrongType = new GumonjiSession(ServerKind.Backd, database.Accounts, database.Characters,
            database.LoginTokens, database.Gameplay, new(), (_, _, _) => throw new Xunit.Sdk.XunitException("unexpected reply"));
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => dispatcher.DispatchAsync(
            PacketType.BackdStatusRequest, new byte[16], wrongType));
        Assert.Contains(nameof(BackdSession), error.Message);
    }
}

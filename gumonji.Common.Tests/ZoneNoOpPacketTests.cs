using gumonji.Network;
using gumonji.Network.Packets.Zone;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace gumonji.Common.Tests;

public sealed class ZoneNoOpPacketTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(252)]
    [InlineData(253)]
    [InlineData(1000)]
    public void ArraySupportsCompactAndExtendedCounts(int count)
    {
        var writer = new PacketWriter();
        writer.WriteCompactCount(count);
        var expected = Enumerable.Range(0, count).Select(i => 0x10203040u + (uint)i).ToArray();
        foreach (var value in expected)
            writer.Write(value);
        Assert.Equal(expected, NoOp1B62Request.FromBytes(writer.ToBytes()).Values);
    }

    [Theory]
    [InlineData("")] // missing count
    [InlineData("fe")] // unsupported prefix
    [InlineData("fd0000")] // truncated extended count
    [InlineData("fd000003e9")] // 1001 entries
    [InlineData("fdffffffff")] // cannot fit a managed count
    [InlineData("01010203")] // truncated element
    [InlineData("0000")] // trailing data
    public void ArrayRejectsMalformedPayloads(string hex)
    {
        Assert.Throws<InvalidDataException>(() => NoOp1B62Request.FromBytes(Convert.FromHexString(hex)));
    }

    [Fact]
    public void FixedFieldsAreBigEndian()
    {
        Assert.Equal(0x12345678u, NoOp1FC2Request.FromBytes([0x12, 0x34, 0x56, 0x78]).Value);
        var pair = NoOp2082Request.FromBytes([0x12, 0x34, 0xab, 0xcd]);
        Assert.Equal((ushort)0x1234, pair.First);
        Assert.Equal((ushort)0xabcd, pair.Second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void FixedFieldsRejectWrongSizes(int length)
    {
        Assert.Throws<InvalidDataException>(() => NoOp1FC2Request.FromBytes(new byte[length]));
        Assert.Throws<InvalidDataException>(() => NoOp2082Request.FromBytes(new byte[length]));
    }

    [Theory]
    [InlineData(PacketType.NoOp1B62Request, "0212345678ffffffff")]
    [InlineData(PacketType.NoOp1FC2Request, "12345678")]
    [InlineData(PacketType.NoOp2082Request, "1234abcd")]
    public async Task RegisteredOnlyForZoneAndSilentEvenBeforeLogin(PacketType opcode, string hex)
    {
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);
        using var database = new TestDatabaseFixture();
        var session = new GumonjiSession(ServerKind.Zone, database.Accounts, database.Characters,
            database.LoginTokens, database.Gameplay, new(), (_, _, _) =>
            throw new Xunit.Sdk.XunitException("ignored packet sent a response"));
        var payload = Convert.FromHexString(hex);
        Assert.False(await dispatcher.DispatchAsync(ServerKind.Femsg, opcode, payload, session));
        Assert.False(await dispatcher.DispatchAsync(ServerKind.Backd, opcode, payload, session));
        Assert.False(session.SilentNoReply);
        Assert.True(await dispatcher.DispatchAsync(ServerKind.Zone, opcode, payload, session));
        Assert.True(session.SilentNoReply);
        Assert.Equal(0, session.SentCount);
        Assert.Equal(SessionState.WaitCheckPassword, session.State);
        Assert.Null(session.UserId);
        Assert.Null(session.CharacterId);
    }
}

namespace gumonji.Network.Packets.Backd;

public sealed record UserOnlineRequest(uint UserId) : IIncomingPacket<UserOnlineRequest>
{
    public static UserOnlineRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var uid = r.ReadUInt32();
        r.ExpectEnd();
        return new(uid);
    }
}

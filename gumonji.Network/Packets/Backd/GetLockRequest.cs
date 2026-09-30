namespace gumonji.Network.Packets.Backd;

public sealed record GetLockRequest(uint MessageId, uint UserId) : IIncomingPacket<GetLockRequest>
{
    public static GetLockRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var msgid = r.ReadUInt32();
        var uid = r.ReadUInt32();
        r.ExpectEnd();
        return new(msgid, uid);
    }
}

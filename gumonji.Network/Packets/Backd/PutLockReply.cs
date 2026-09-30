namespace gumonji.Network.Packets.Backd;

public sealed record PutLockReply(uint MessageId, uint Result, uint UserId) : IOutgoingPacket
{
    public PacketType Type => PacketType.BackdPutLockReply;

    public byte[] ToBytes()
    {
        var w = new PacketWriter();
        w.Write(MessageId);
        w.Write(Result);
        w.Write(UserId);
        return w.ToBytes();
    }
}

namespace gumonji.Network.Packets.Backd;

public sealed record SaveHistoryReply(uint Result) : IOutgoingPacket
{
    public PacketType Type => PacketType.BackdSaveHistoryReply;

    public byte[] ToBytes()
    {
        var w = new PacketWriter();
        w.Write(Result);
        return w.ToBytes();
    }
}

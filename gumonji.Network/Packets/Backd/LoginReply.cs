namespace gumonji.Network.Packets.Backd;

public sealed record LoginReply(uint Result, uint BackendValue, byte[] Text, uint Approval) : IOutgoingPacket
{
    public PacketType Type => PacketType.BackdLoginReply;

    public byte[] ToBytes()
    {
        var w = new PacketWriter();
        w.Write(Result);
        w.Write(BackendValue);
        w.WriteCompactBytes(Text);
        w.Write(Approval);
        return w.ToBytes();
    }
}

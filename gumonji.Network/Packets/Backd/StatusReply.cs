namespace gumonji.Network.Packets.Backd;

public sealed record StatusReply(uint Version, uint Build, uint Result) : IOutgoingPacket
{
    public PacketType Type => PacketType.BackdStatusReply;

    public byte[] ToBytes()
    {
        var w = new PacketWriter();
        w.Write(Version);
        w.Write(Build);
        w.Write(Result);
        return w.ToBytes();
    }
}

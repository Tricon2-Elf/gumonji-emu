namespace gumonji.Network.Packets.Backd;

public sealed record VendorZonesReply(uint MessageId) : IOutgoingPacket
{
    public PacketType Type => PacketType.BackdVendorZonesReply;

    public byte[] ToBytes()
    {
        var w = new PacketWriter();
        w.Write(MessageId);
        w.Write(0u);
        w.WriteCompactCount(0);
        return w.ToBytes();
    }
}

namespace gumonji.Network.Packets.Femsg;

public sealed class HomeZoneReply(uint zoneId) : IOutgoingPacket
{
    public PacketType Type => PacketType.HomeZoneReply;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(zoneId);
        return writer.ToBytes();
    }
}

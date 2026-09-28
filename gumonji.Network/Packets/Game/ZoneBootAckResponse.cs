namespace gumonji.Network.Packets.Game;

public sealed class ZoneBootAckResponse : IOutgoingPacket
{
    public PacketType Type => PacketType.ZoneBootAckResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(0u);
        writer.WriteCompactBytes([]);
        return writer.ToBytes();
    }
}

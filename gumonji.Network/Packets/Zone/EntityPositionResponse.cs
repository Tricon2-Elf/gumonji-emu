namespace gumonji.Network.Packets.Zone;

public sealed class EntityPositionResponse(uint status, uint entityId, uint x, uint y) : IOutgoingPacket
{
    public PacketType Type => PacketType.EntityPositionResponse;
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(status); writer.Write(entityId); writer.Write(x); writer.Write(y);
        return writer.ToBytes();
    }
}

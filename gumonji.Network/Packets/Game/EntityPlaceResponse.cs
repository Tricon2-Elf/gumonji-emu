namespace gumonji.Network.Packets.Game;

public sealed class EntityPlaceResponse(uint entityId, ushort x = 64, ushort y = 64) : IOutgoingPacket
{
    public PacketType Type => PacketType.EntityPlaceResponse;

    public byte[] ToBytes()
    {
        if (entityId is 0 or > 0x7FFFFFFF)
            throw new InvalidDataException("entity id or position is out of range");
        var writer = new PacketWriter();
        writer.Write(entityId);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write(0u);
        writer.Write((byte)1);
        writer.Write((byte)6);
        writer.Write((uint)x * 1000);
        writer.Write((uint)y * 1000);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write((byte)0);
        // Player actors must be registered in the normal world-entity table.
        // Chat and emote events resolve their target from this table (0x20),
        // rather than the generic placement table selected by zero.
        writer.Write((byte)0x20);
        writer.Write(0u);
        writer.Write(0u);
        return writer.ToBytes();
    }
}

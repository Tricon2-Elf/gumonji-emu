namespace gumonji.Network.Packets.Zone;

public sealed class EntityPositionRequest(uint entityId) : IIncomingPacket<EntityPositionRequest>
{
    public uint EntityId { get; } = entityId;
    public static EntityPositionRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 4) throw new InvalidDataException("invalid INFO_POSITION size");
        return new(new PacketReader(data).ReadUInt32());
    }
}

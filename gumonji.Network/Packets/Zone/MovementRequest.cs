namespace gumonji.Network.Packets.Zone;

public sealed class MovementRequest(uint entityId) : IIncomingPacket<MovementRequest>
{
    public uint EntityId { get; } = entityId;

    public static MovementRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < 10)
            throw new InvalidDataException("truncated MOVEMENT");
        var reader = new PacketReader(data);
        var entityId = reader.ReadUInt32();
        _ = reader.ReadByte();
        _ = reader.ReadUInt32();
        _ = reader.ReadByte();
        var count = reader.ReadByte();
        if (count > 6 || reader.Remaining != (count * 4) + 13)
            throw new InvalidDataException("invalid MOVEMENT");
        return new MovementRequest(entityId);
    }
}

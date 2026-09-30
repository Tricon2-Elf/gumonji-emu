namespace gumonji.Network.Packets.Zone;

public sealed class ChunkSubscribeRequest(uint x, uint y) : IIncomingPacket<ChunkSubscribeRequest>
{
    public uint X { get; } = x;
    public uint Y { get; } = y;

    public static ChunkSubscribeRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 8)
            throw new InvalidDataException("invalid CHUNK_SUBSCRIBE size");
        var reader = new PacketReader(data);
        return new ChunkSubscribeRequest(reader.ReadUInt32(), reader.ReadUInt32());
    }
}

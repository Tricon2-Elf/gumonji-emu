namespace gumonji.Network.Packets.Game;

public sealed class GetPageDataRequest(uint chunkX, uint chunkY) : IIncomingPacket<GetPageDataRequest>
{
    public uint ChunkX { get; } = chunkX;
    public uint ChunkY { get; } = chunkY;

    public static GetPageDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 8)
            throw new InvalidDataException("GET_PAGE_DATA before zone entry or truncated");
        var reader = new PacketReader(data);
        return new GetPageDataRequest(reader.ReadUInt32(), reader.ReadUInt32());
    }
}

namespace gumonji.Network.Packets.Game;

public sealed class ChunkLoadedRequest : IIncomingPacket<ChunkLoadedRequest>
{
    public static ChunkLoadedRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
            throw new InvalidDataException("invalid CHUNK_LOADED size");
        return new ChunkLoadedRequest();
    }
}

namespace gumonji.Network.Packets.Game;

public sealed class StringListRequest : IIncomingPacket<StringListRequest>
{
    public static StringListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
            throw new InvalidDataException("invalid STRING_LIST_REQUEST size");
        return new StringListRequest();
    }
}

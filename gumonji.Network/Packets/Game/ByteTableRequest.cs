namespace gumonji.Network.Packets.Game;

public sealed class ByteTableRequest : IIncomingPacket<ByteTableRequest>
{
    public static ByteTableRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
            throw new InvalidDataException("invalid BYTE_TABLE_REQUEST size");
        return new ByteTableRequest();
    }
}

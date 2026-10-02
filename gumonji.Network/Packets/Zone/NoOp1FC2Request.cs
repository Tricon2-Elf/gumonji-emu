namespace gumonji.Network.Packets.Zone;

public sealed class NoOp1FC2Request(uint value) : IIncomingPacket<NoOp1FC2Request>
{
    public uint Value { get; } = value;

    public static NoOp1FC2Request FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 4)
            throw new InvalidDataException("invalid UNNAMED_1FC2 size");
        return new NoOp1FC2Request(new PacketReader(data).ReadUInt32());
    }
}

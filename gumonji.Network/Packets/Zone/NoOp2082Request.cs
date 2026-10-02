namespace gumonji.Network.Packets.Zone;

public sealed class NoOp2082Request(ushort first, ushort second) : IIncomingPacket<NoOp2082Request>
{
    public ushort First { get; } = first;
    public ushort Second { get; } = second;

    public static NoOp2082Request FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 4)
            throw new InvalidDataException("invalid UNNAMED_2082 size");
        var reader = new PacketReader(data);
        return new NoOp2082Request(reader.ReadUInt16(), reader.ReadUInt16());
    }
}

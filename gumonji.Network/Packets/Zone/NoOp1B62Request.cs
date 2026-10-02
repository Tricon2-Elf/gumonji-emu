namespace gumonji.Network.Packets.Zone;

public sealed class NoOp1B62Request(uint[] values) : IIncomingPacket<NoOp1B62Request>
{
    public uint[] Values { get; } = values;

    public static NoOp1B62Request FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var count = reader.ReadCompactLength();
        if (count > 1000 || reader.Remaining != count * 4)
            throw new InvalidDataException("invalid UNNAMED_1B62 array size");
        var values = new uint[count];
        for (var i = 0; i < count; i++)
            values[i] = reader.ReadUInt32();
        return new NoOp1B62Request(values);
    }
}

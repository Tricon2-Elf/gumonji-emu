namespace gumonji.Network.Packets.Femsg;

public sealed class ProgressValueRequest(uint value) : IIncomingPacket<ProgressValueRequest>
{
    public uint Value { get; } = value;

    public static ProgressValueRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var value = reader.ReadUInt32();
        reader.ExpectEnd();
        return new ProgressValueRequest(value);
    }
}

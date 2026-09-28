namespace gumonji.Network.Packets.Femsg;

public sealed class HeartbeatRequest(byte[] fields) : IIncomingPacket<HeartbeatRequest>
{
    public byte[] Fields { get; } = fields;

    public static HeartbeatRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 16)
            throw new InvalidDataException("invalid frontend heartbeat size");
        return new HeartbeatRequest(data.ToArray());
    }
}

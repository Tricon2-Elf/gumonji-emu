namespace gumonji.Network.Packets.Zone;

public sealed class PingRequest(byte[] echoed) : IIncomingPacket<PingRequest>
{
    public byte[] Echoed { get; } = echoed;

    public static PingRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 20)
            throw new InvalidDataException("invalid game PING size");
        return new PingRequest(data[..12].ToArray());
    }
}

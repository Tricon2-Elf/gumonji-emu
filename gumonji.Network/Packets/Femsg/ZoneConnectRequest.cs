namespace gumonji.Network.Packets.Femsg;

public sealed class ZoneConnectRequest(byte[] zone) : IIncomingPacket<ZoneConnectRequest>
{
    public byte[] Zone { get; } = zone;

    public static ZoneConnectRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var zone = reader.ReadCompactBytes();
        _ = reader.ReadUInt32();
        if (zone.Length is 0 or > 16 || reader.Remaining != 0)
            throw new InvalidDataException("invalid zone request");
        return new ZoneConnectRequest(zone);
    }
}

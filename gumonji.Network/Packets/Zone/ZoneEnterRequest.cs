namespace gumonji.Network.Packets.Zone;

public sealed class ZoneEnterRequest(byte[] path, byte[] extra, uint zoneId) : IIncomingPacket<ZoneEnterRequest>
{
    public byte[] Path { get; } = path;
    public byte[] Extra { get; } = extra;
    public uint ZoneId { get; } = zoneId;

    public static ZoneEnterRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var path = reader.ReadCompactBytes();
        var extra = reader.ReadCompactBytes();
        var zoneId = reader.ReadUInt32();
        if (reader.Remaining != 0 || path.Length > 256 || extra.Length > 1024)
            throw new InvalidDataException("invalid ZONE_ENTER_REQUEST");
        return new ZoneEnterRequest(path, extra, zoneId);
    }
}

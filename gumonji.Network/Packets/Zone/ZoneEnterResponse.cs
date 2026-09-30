namespace gumonji.Network.Packets.Zone;

public sealed class ZoneEnterResponse(ushort x = 64, ushort y = 64, byte[]? title = null) : IOutgoingPacket
{
    public PacketType Type => PacketType.ZoneEnterResponse;

    public byte[] ToBytes()
    {
        var name = title ?? "1"u8.ToArray();
        if (name.Length > 127)
            throw new InvalidDataException("zone enter position or title is out of range");
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(x);
        writer.Write(y);
        writer.WriteCompactBytes([]);
        writer.WriteCompactBytes(name);
        return writer.ToBytes();
    }
}

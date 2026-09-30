namespace gumonji.Network.Packets.Zone;

public sealed class ClockSyncResponse(byte day, byte hour, byte minute, byte scale = 1) : IOutgoingPacket
{
    public PacketType Type => PacketType.ClockSyncResponse;

    public byte[] ToBytes()
    {
        if (hour > 23 || minute > 59 || scale == 0)
            throw new InvalidDataException("game clock fields out of range");
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write((byte)0);
        writer.Write(day);
        writer.Write(hour);
        writer.Write(minute);
        writer.Write(scale);
        return writer.ToBytes();
    }
}

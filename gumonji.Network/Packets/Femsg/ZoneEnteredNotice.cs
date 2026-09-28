namespace gumonji.Network.Packets.Femsg;

public sealed class ZoneEnteredNotice : IIncomingPacket<ZoneEnteredNotice>
{
    public static ZoneEnteredNotice FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
            throw new InvalidDataException("invalid ZONE_ENTERED_NOTICE size");
        return new ZoneEnteredNotice();
    }
}

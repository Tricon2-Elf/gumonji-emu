namespace gumonji.Network.Packets.Game;

public sealed class ZoneAnnounceRequest : IIncomingPacket<ZoneAnnounceRequest>
{
    public static ZoneAnnounceRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
            throw new InvalidDataException("invalid ZONE_ANNOUNCE size");
        return new ZoneAnnounceRequest();
    }
}

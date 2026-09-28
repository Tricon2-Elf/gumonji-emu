namespace gumonji.Network.Packets.Femsg;

public sealed class HomeZoneRequest : IIncomingPacket<HomeZoneRequest>
{
    public static HomeZoneRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
            throw new InvalidDataException("HOME_ZONE_REQUEST has unexpected fields");
        return new HomeZoneRequest();
    }
}

namespace gumonji.Network.Packets.Game;

public sealed class ZoneBootRequest : IIncomingPacket<ZoneBootRequest>
{
    public static ZoneBootRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
            throw new InvalidDataException("invalid ZONE_BOOT_REQUEST size");
        return new ZoneBootRequest();
    }
}

namespace gumonji.Network.Packets.Backd;

public sealed record PassageLinksRequest(uint ZoneId, uint Width, uint Height) : IIncomingPacket<PassageLinksRequest>
{
    public static PassageLinksRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var zoneId = r.ReadUInt32();
        var width = r.ReadUInt32();
        var height = r.ReadUInt32();
        r.ExpectEnd();
        return new(zoneId, width, height);
    }
}

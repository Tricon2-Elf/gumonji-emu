namespace gumonji.Network.Packets.Backd;

public sealed record SellerIdsRequest(uint[] Ids) : IIncomingPacket<SellerIdsRequest>
{
    public static SellerIdsRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var ids = BackdPacketFields.ReadCompactArray(ref r, 2048);
        r.ExpectEnd();
        return new(ids);
    }
}

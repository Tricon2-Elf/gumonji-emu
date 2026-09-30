namespace gumonji.Network.Packets.Zone;

// Client receive case 0x0777 reads status then item id (sub_4FA9B0).
public sealed record ItemPickupResponse(uint ItemId, bool Success) : IOutgoingPacket
{
    public PacketType Type => PacketType.ItemPickupResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Success ? 0u : unchecked((uint)-67));
        writer.Write(ItemId);
        return writer.ToBytes();
    }
}

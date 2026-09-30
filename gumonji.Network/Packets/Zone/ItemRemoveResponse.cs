namespace gumonji.Network.Packets.Zone;

// Client receive case 0x206F calls sub_4FD190 to delete the world item.
public sealed record ItemRemoveResponse(uint ItemId) : IOutgoingPacket
{
    public PacketType Type => PacketType.ItemRemoveResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ItemId);
        return writer.ToBytes();
    }
}

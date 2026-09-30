namespace gumonji.Network.Packets.Game;

// Client sender sub_44D820: one world-item id.
public sealed record ItemPickupRequest(uint ItemId) : IIncomingPacket<ItemPickupRequest>
{
    public static ItemPickupRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var request = new ItemPickupRequest(reader.ReadUInt32());
        reader.ExpectEnd();
        return request;
    }
}

namespace gumonji.Network.Packets.Zone;

// Client receiver 0x077D calls sub_4FAB30; the middle u32 is status.
public sealed record ItemUseResponse(uint Slot, bool Success) : IOutgoingPacket
{
    public PacketType Type => PacketType.ItemUseResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(Success ? 0u : 1u);
        writer.Write(Slot);
        return writer.ToBytes();
    }
}

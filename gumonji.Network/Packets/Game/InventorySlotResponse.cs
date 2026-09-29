namespace gumonji.Network.Packets.Game;

// Client 0x43D526 -> 0x4FA5C0. These are item attributes, NOT chat fields.
public sealed record InventorySlotResponse(byte Slot, uint ItemId, ushort ItemType, byte Subtype,
    byte Color, uint Fertility) : IOutgoingPacket
{
    public PacketType Type => PacketType.InventorySlotResponse;

    public byte[] ToBytes()
    {
        if (Slot >= 48)
            throw new InvalidDataException("inventory slot out of range");
        var writer = new PacketWriter();
        writer.Write(Slot);
        writer.Write(ItemId);
        writer.Write(ItemType);
        writer.Write(Subtype);
        writer.Write(Color);
        writer.Write(Fertility);
        writer.Write(0u);
        writer.Write(0u);
        writer.WriteCompactCount(8);
        for (var i = 0; i < 8; i++)
            writer.Write(0u);
        writer.WriteCompactCount(8);
        for (var i = 0; i < 8; i++)
            writer.WriteCompactBytes([]);
        writer.Write(0u); // optional source actor
        writer.WriteCompactBytes([]); // custom name (use the client's template name)
        return writer.ToBytes();
    }
}

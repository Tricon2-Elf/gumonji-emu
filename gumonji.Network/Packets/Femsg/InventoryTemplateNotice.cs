namespace gumonji.Network.Packets.Femsg;

/// <summary>
/// Frontend opcode 419, sent from sub_505F10 after the local avatar is updated.
/// The three equal-length arrays contain item template, subtype, and color.
/// </summary>
public sealed record InventoryTemplateNotice(ushort[] ItemTypes, byte[] Subtypes, byte[] Colors)
    : IIncomingPacket<InventoryTemplateNotice>
{
    public static InventoryTemplateNotice FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var count = reader.ReadCompactLength();
        if (count > 16)
            throw new InvalidDataException("inventory template count out of range");
        var itemTypes = new ushort[count];
        for (var i = 0; i < count; i++)
            itemTypes[i] = reader.ReadUInt16();
        var subtypes = reader.ReadCompactBytes();
        var colors = reader.ReadCompactBytes();
        if (subtypes.Length != count || colors.Length != count)
            throw new InvalidDataException("inventory template array counts differ");
        reader.ExpectEnd();
        return new InventoryTemplateNotice(itemTypes, subtypes, colors);
    }
}

namespace gumonji.Network.Packets.Zone;

public sealed record RenderItem(byte Slot, ushort Type, byte Subtype, byte Color);

/// <summary>sub_440F20: own render configuration; account id, not entity id.</summary>
public sealed class RenderTaskResponse(uint userId, int body, int model, int style,
    byte badge, IReadOnlyList<RenderItem> items) : IOutgoingPacket
{
    public PacketType Type => PacketType.RenderTaskResponse;
    public byte[] ToBytes()
    {
        if (items.Count > 48 || items.Select(i => i.Slot).Distinct().Count() != items.Count ||
            items.Any(i => i.Slot >= 48))
            throw new InvalidDataException("invalid render item slots");
        var appearance = PlayerAppearance.Resolve(body, model, style, 0);
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(appearance.Type); writer.Write(appearance.Subtype);
        writer.Write(appearance.ColorType); writer.Write(appearance.Eye);
        writer.Write(userId); writer.Write(badge);
        writer.WriteCompactBytes(items.Select(i => i.Slot).ToArray());
        writer.WriteCompactCount(items.Count);
        foreach (var item in items) writer.Write(item.Type);
        writer.WriteCompactBytes(items.Select(i => i.Subtype).ToArray());
        writer.WriteCompactBytes(items.Select(i => i.Color).ToArray());
        return writer.ToBytes();
    }
}

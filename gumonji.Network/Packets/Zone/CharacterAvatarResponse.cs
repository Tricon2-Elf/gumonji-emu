namespace gumonji.Network.Packets.Zone;

public sealed class CharacterAvatarResponse(
    uint entityId,
    byte[] name,
    int body,
    int model,
    int style,
    int color,
    byte? vehicleSlot = null,
    uint vehicleId = 0) : IOutgoingPacket
{
    public PacketType Type => PacketType.CharacterAvatarResponse;

    public byte[] ToBytes()
    {
        if (entityId is 0 or > 0x7FFFFFFF || name.Length > 128)
            throw new InvalidDataException("entity id or character name is out of range");
        var subtype = body is 0 or 1 ? body : 0;
        var colortype = model is >= 0 and <= 16 ? model : 0;
        var eye = style >= 0 && style < PlayerAppearance.StyleEyes.Length ? PlayerAppearance.StyleEyes[style] : 0;
        var tint = color is >= 0 and < 100 ? color : 0;
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(entityId);
        writer.Write((byte)1);
        writer.Write((byte)subtype);
        writer.Write((byte)colortype);
        writer.Write((byte)eye);
        writer.WriteCompactBytes(name);
        writer.Write(0u);
        writer.Write((byte)0);
        writer.Write(0u);
        writer.Write(0u);
        writer.WriteCompactCount(0);
        writer.WriteCompactCount(0);
        writer.Write((byte)tint);
        if (vehicleSlot is { } slot && vehicleId != 0)
        {
            // sub_4F7B50 reads these as inventory slot, item type,
            // subtype, color, item id, and eight item attributes.
            writer.WriteCompactBytes([slot]);
            writer.WriteCompactCount(1);
            writer.Write(ItemTemplateIds.ToyCar);
            writer.WriteCompactBytes([0]);
            writer.WriteCompactBytes([0]);
            writer.WriteCompactCount(1);
            writer.Write(vehicleId);
            writer.WriteCompactCount(8);
            for (var i = 0; i < 8; i++)
                writer.Write(0u);
        }
        else
        {
            for (var i = 0; i < 6; i++)
                writer.WriteCompactCount(0);
        }
        writer.Write(0u);
        writer.WriteCompactCount(0);
        writer.Write((byte)0);
        writer.Write((byte)0);
        return writer.ToBytes();
    }
}

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
        var appearance = PlayerAppearance.Resolve(body, model, style, color);
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(entityId);
        writer.Write(appearance.Type);
        writer.Write(appearance.Subtype);
        writer.Write(appearance.ColorType);
        writer.Write(appearance.Eye);
        writer.WriteCompactBytes(name);
        writer.Write(0u);
        writer.Write((byte)0);
        writer.Write(0u);
        writer.Write(0u);
        writer.WriteCompactCount(0);
        writer.WriteCompactCount(0);
        writer.Write(appearance.Tint);
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

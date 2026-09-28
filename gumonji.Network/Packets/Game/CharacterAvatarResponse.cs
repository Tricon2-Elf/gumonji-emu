namespace gumonji.Network.Packets.Game;

public sealed class CharacterAvatarResponse(
    uint entityId,
    byte[] name,
    int body,
    int model,
    int style,
    int color) : IOutgoingPacket
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
        for (var i = 0; i < 6; i++)
            writer.WriteCompactCount(0);
        writer.Write(0u);
        writer.WriteCompactCount(0);
        writer.Write((byte)0);
        writer.Write((byte)0);
        return writer.ToBytes();
    }
}

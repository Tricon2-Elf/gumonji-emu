namespace gumonji.Network.Packets.Zone;

/// <summary>Client receive case 0x206D (0x445DF5) calls sub_4FC3C0.</summary>
public sealed record ItemPlaceResponse(uint ItemId, ushort X, ushort Y) : IOutgoingPacket
{
    public PacketType Type => PacketType.ItemPlaceResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ItemId);
        writer.Write(ItemTemplateIds.ToyCar); // item.tmpl: toycar_black, subtype 0, black
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write(1000u); // item fertility
        writer.Write(0u); // owner
        writer.WriteCompactCount(8); // client consumes all eight item attributes
        for (var i = 0; i < 8; i++)
            writer.Write(0u);
        writer.Write(X);
        writer.Write(Y);
        writer.Write((byte)0); // rotation
        for (var i = 0; i < 4; i++)
            writer.Write(0u);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.WriteCompactBytes([]); // custom name
        return writer.ToBytes();
    }
}

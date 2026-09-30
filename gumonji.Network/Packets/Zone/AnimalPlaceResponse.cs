namespace gumonji.Network.Packets.Zone;

/// <summary>
/// Client receive case 0x2009 (0x444CA3) calls sub_4FDD90. Its animal
/// type/subtype/color are looked up in animal.tmpl, and repeated packets
/// with the same id update the creature's position.
/// </summary>
public sealed record AnimalPlaceResponse(uint AnimalId, ushort X, ushort Y) : IOutgoingPacket
{
    public PacketType Type => PacketType.AnimalPlaceResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(AnimalId);
        writer.Write((byte)3); // animal.tmpl parser: mammal=3 (reptile=1)
        writer.Write((byte)0); // cow_black
        writer.Write((byte)0); // black
        writer.Write((byte)0); // standing
        writer.Write((byte)0);
        writer.Write(10000u); // current fertility
        writer.Write(10000u); // maximum fertility
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u); // no parent/attached entity
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write(X);
        writer.Write(Y);
        for (var i = 0; i < 5; i++)
            writer.Write(0u);
        writer.WriteCompactBytes([]); // custom name
        writer.WriteCompactBytes([]); // gumonji id
        return writer.ToBytes();
    }
}

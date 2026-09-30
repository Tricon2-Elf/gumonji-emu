namespace gumonji.Network.Packets.Zone;

// Client 0x44117C -> 0x501170. No leading entity ID (the other-character
// condition packet has one). Keep unimplemented economy/edit fields at zero.
public sealed record CharacterConditionResponse(byte[] Name, uint PlayedSeconds, uint Walking, uint Swimming)
    : IOutgoingPacket
{
    public PacketType Type => PacketType.CharacterConditionResponse;

    public byte[] ToBytes()
    {
        if (Name.Length > 128)
            throw new InvalidDataException("condition name exceeds client capacity");
        var writer = new PacketWriter();
        writer.WriteCompactBytes(Name);
        uint[] fields = [0, 0, 0, 0, PlayedSeconds, Walking, Swimming, 0, 0, 0, 0, 0, 0, 0];
        foreach (var field in fields)
            writer.Write(field);
        writer.WriteCompactBytes(Name);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);
        return writer.ToBytes();
    }
}

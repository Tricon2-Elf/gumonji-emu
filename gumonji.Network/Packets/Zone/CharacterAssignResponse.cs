namespace gumonji.Network.Packets.Zone;

public sealed class CharacterAssignResponse(uint characterId) : IOutgoingPacket
{
    public PacketType Type => PacketType.CharacterAssignResponse;

    public byte[] ToBytes()
    {
        if (characterId is 0 or > 0x7FFFFFFF)
            throw new InvalidDataException("character id must be a positive signed 32-bit value");
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(characterId);
        return writer.ToBytes();
    }
}

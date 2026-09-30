namespace gumonji.Network.Packets.Game;

/// <summary>Status 0 opens character creation; status 1 asks the client to load its saved character.</summary>
public sealed class CharacterCheckExistResponse(bool exists) : IOutgoingPacket
{
    public PacketType Type => PacketType.CharacterCheckExistResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(exists ? 1u : 0u);
        writer.WriteCompactBytes([]);
        return writer.ToBytes();
    }
}

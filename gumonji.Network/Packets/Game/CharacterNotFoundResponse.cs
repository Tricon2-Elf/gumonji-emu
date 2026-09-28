namespace gumonji.Network.Packets.Game;

public sealed class CharacterNotFoundResponse : IOutgoingPacket
{
    public PacketType Type => PacketType.CharacterNotFoundResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.WriteCompactBytes([]);
        return writer.ToBytes();
    }
}

namespace gumonji.Network.Packets.Zone;

public sealed class CharacterFieldAckResponse : IOutgoingPacket
{
    public PacketType Type => PacketType.CharacterFieldAckResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(0u);
        return writer.ToBytes();
    }
}

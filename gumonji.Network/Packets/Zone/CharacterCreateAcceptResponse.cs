namespace gumonji.Network.Packets.Zone;

public sealed class CharacterCreateAcceptResponse : IOutgoingPacket
{
    public PacketType Type => PacketType.CharacterCreateAcceptResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(0u);
        return writer.ToBytes();
    }
}

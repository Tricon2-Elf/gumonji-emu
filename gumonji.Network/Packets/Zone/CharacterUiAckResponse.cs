namespace gumonji.Network.Packets.Zone;

public sealed class CharacterUiAckResponse : IOutgoingPacket
{
    public PacketType Type => PacketType.CharacterUiAckResponse;
    public byte[] ToBytes() => [];
}

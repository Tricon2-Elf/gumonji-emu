namespace gumonji.Network.Packets.Game;

public sealed class CharacterUiAckResponse : IOutgoingPacket
{
    public PacketType Type => PacketType.CharacterUiAckResponse;
    public byte[] ToBytes() => [];
}

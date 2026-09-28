namespace gumonji.Network.Packets.Game;

public sealed class StringListResponse : IOutgoingPacket
{
    public PacketType Type => PacketType.StringListResponse;
    public byte[] ToBytes() => [0x00];
}

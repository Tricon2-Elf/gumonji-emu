namespace gumonji.Network.Packets.Zone;

public sealed class StringListResponse : IOutgoingPacket
{
    public PacketType Type => PacketType.StringListResponse;
    public byte[] ToBytes() => [0x00];
}

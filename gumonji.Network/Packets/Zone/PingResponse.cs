namespace gumonji.Network.Packets.Zone;

public sealed class PingResponse(byte[] echoed) : IOutgoingPacket
{
    public PacketType Type => PacketType.PingResponse;
    public byte[] ToBytes() => echoed;
}

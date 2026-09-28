namespace gumonji.Network.Packets.Game;

public sealed class PingResponse(byte[] echoed) : IOutgoingPacket
{
    public PacketType Type => PacketType.PingResponse;
    public byte[] ToBytes() => echoed;
}

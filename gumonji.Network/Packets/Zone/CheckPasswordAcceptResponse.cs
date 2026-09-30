namespace gumonji.Network.Packets.Zone;

public sealed class CheckPasswordAcceptResponse(uint userId) : IOutgoingPacket
{
    public PacketType Type => PacketType.CheckPasswordAcceptResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write((ushort)1);
        writer.Write((ushort)128);
        writer.Write((ushort)128);
        writer.Write(userId);
        return writer.ToBytes();
    }
}

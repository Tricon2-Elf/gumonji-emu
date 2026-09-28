namespace gumonji.Network.Packets.Game;

public sealed class ByteTableResponse : IOutgoingPacket
{
    public PacketType Type => PacketType.ByteTableResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        var column = new byte[] { 0x00 };
        for (var i = 0; i < 4; i++)
            writer.WriteCompactBytes(column);
        return writer.ToBytes();
    }
}

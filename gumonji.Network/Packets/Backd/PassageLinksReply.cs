namespace gumonji.Network.Packets.Backd;

public sealed record PassageLinksReply(uint ZoneId) : IOutgoingPacket
{
    public PacketType Type => PacketType.BackdPassageLinksReply;

    public byte[] ToBytes()
    {
        var w = new PacketWriter();
        w.Write(ZoneId);
        for (var array = 0; array < 8; array++)
        {
            w.WriteCompactCount(6);
            for (var index = 0; index < 6; index++) w.Write(0u);
        }
        return w.ToBytes();
    }
}

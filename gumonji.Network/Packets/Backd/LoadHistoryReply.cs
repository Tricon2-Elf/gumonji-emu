namespace gumonji.Network.Packets.Backd;

public sealed record LoadHistoryReply(uint MessageId, uint UserId, uint[] Values) : IOutgoingPacket
{
    public PacketType Type => PacketType.BackdLoadHistoryReply;

    public byte[] ToBytes()
    {
        var w = new PacketWriter();
        w.Write(MessageId);
        w.Write(UserId);
        BackdPacketFields.WriteArray(w, Values);
        return w.ToBytes();
    }
}

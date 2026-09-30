namespace gumonji.Network.Packets.Backd;

public sealed record LoadCharacterReply(uint MessageId, uint UserId, uint Result, byte[] Payload, uint[] Options) : IOutgoingPacket
{
    public PacketType Type => PacketType.BackdLoadCharacterReply;

    public byte[] ToBytes()
    {
        var w = new PacketWriter();
        w.Write(MessageId);
        w.Write(UserId);
        w.Write(Result);
        w.WriteCompactBytes(Payload);
        BackdPacketFields.WriteArray(w, Options);
        return w.ToBytes();
    }
}

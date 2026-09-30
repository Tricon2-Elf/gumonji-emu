namespace gumonji.Network.Packets.Backd;

public sealed record SaveCharacterReply(uint MessageId, uint Result, uint Operation) : IOutgoingPacket
{
    public PacketType Type => PacketType.BackdSaveCharacterReply;

    public byte[] ToBytes()
    {
        var w = new PacketWriter();
        w.Write(MessageId);
        w.Write(Result);
        w.Write(Operation);
        return w.ToBytes();
    }
}

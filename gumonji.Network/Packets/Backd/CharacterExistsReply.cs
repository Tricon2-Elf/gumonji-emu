namespace gumonji.Network.Packets.Backd;

public sealed record CharacterExistsReply(uint MessageId, uint Result) : IOutgoingPacket
{
    public PacketType Type => PacketType.BackdCharacterExistsReply;

    public byte[] ToBytes()
    {
        var w = new PacketWriter();
        w.Write(MessageId);
        w.Write(Result);
        return w.ToBytes();
    }
}

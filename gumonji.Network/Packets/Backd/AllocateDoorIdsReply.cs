namespace gumonji.Network.Packets.Backd;

public sealed record AllocateDoorIdsReply(uint[] Ids) : IOutgoingPacket
{
    public PacketType Type => PacketType.BackdAllocateDoorIdsReply;

    public byte[] ToBytes()
    {
        var w = new PacketWriter();
        BackdPacketFields.WriteArray(w, Ids);
        return w.ToBytes();
    }
}

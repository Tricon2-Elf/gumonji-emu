namespace gumonji.Network.Packets.Backd;

public sealed record CheckPasswordReply(uint MessageId, uint Result, uint UserId, uint Reserved, uint Approval, byte[] Username, byte[] Permissions, ushort LandEditEntitlement) : IOutgoingPacket
{
    public PacketType Type => PacketType.BackdCheckPasswordReply;

    public byte[] ToBytes()
    {
        var w = new PacketWriter();
        w.Write(MessageId);
        w.Write(Result);
        w.Write(UserId);
        w.Write(Reserved);
        w.Write(Approval);
        w.WriteCompactBytes(Username);
        w.WriteCompactBytes(Permissions);
        w.Write(LandEditEntitlement);
        return w.ToBytes();
    }
}

namespace gumonji.Network.Packets.Backd;

public sealed record CheckPasswordRequest(uint MessageId, uint UserId, byte[] Token) : IIncomingPacket<CheckPasswordRequest>
{
    public static CheckPasswordRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var msgid = r.ReadUInt32();
        var uid = r.ReadUInt32();
        var token = BackdPacketFields.ReadBytes(ref r, 128);
        r.ExpectEnd();
        return new(msgid, uid, token);
    }
}

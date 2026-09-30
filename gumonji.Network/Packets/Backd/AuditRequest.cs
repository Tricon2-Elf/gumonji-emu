namespace gumonji.Network.Packets.Backd;

public sealed record AuditRequest(uint Code, uint UserId, byte[] Name, byte[] Message) : IIncomingPacket<AuditRequest>
{
    public static AuditRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var code = r.ReadUInt32();
        var uid = r.ReadUInt32();
        var name = BackdPacketFields.ReadBytes(ref r, 128);
        var message = BackdPacketFields.ReadBytes(ref r, 4096);
        r.ExpectEnd();
        return new(code, uid, name, message);
    }
}

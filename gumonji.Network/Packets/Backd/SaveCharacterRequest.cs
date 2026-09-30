namespace gumonji.Network.Packets.Backd;

public sealed record SaveCharacterRequest(uint MessageId, uint UserId, byte[] Payload, uint Operation) : IIncomingPacket<SaveCharacterRequest>
{
    public static SaveCharacterRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var msgid = r.ReadUInt32();
        var uid = r.ReadUInt32();
        var payload = BackdPacketFields.ReadBytes(ref r, 262144);
        var operation = r.ReadUInt32();
        r.ExpectEnd();
        return new(msgid, uid, payload, operation);
    }
}

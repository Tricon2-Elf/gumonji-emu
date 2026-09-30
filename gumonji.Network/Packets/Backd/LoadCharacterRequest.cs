namespace gumonji.Network.Packets.Backd;

public sealed record LoadCharacterRequest(uint MessageId, uint UserId, uint[] Options) : IIncomingPacket<LoadCharacterRequest>
{
    public static LoadCharacterRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var msgid = r.ReadUInt32();
        var uid = r.ReadUInt32();
        var options = BackdPacketFields.ReadCompactArray(ref r, 2);
        r.ExpectEnd();
        return new(msgid, uid, options);
    }
}

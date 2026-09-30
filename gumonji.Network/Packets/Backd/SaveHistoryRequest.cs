namespace gumonji.Network.Packets.Backd;

public sealed record SaveHistoryRequest(uint[] Values) : IIncomingPacket<SaveHistoryRequest>
{
    public static SaveHistoryRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var values = BackdPacketFields.ReadArray(ref r, 24);
        r.ExpectEnd();
        return new(values);
    }
}

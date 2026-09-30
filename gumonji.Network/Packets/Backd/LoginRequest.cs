namespace gumonji.Network.Packets.Backd;

public sealed record LoginRequest(byte[] ZoneName, byte[] Password, ushort Port, uint Address) : IIncomingPacket<LoginRequest>
{
    public static LoginRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var name = BackdPacketFields.ReadBytes(ref r, 16);
        var password = BackdPacketFields.ReadBytes(ref r, 128);
        var port = r.ReadUInt16();
        var address = r.ReadUInt32();
        r.ExpectEnd();
        return new(name, password, port, address);
    }
}

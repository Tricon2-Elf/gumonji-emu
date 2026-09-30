namespace gumonji.Network.Packets.Backd;

public sealed record StatusRequest(uint Version, uint Build, uint Value2, uint Value3) : IIncomingPacket<StatusRequest>
{
    public static StatusRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var version = r.ReadUInt32();
        var build = r.ReadUInt32();
        var value2 = r.ReadUInt32();
        var value3 = r.ReadUInt32();
        r.ExpectEnd();
        return new(version, build, value2, value3);
    }
}

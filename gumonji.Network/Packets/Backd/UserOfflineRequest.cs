namespace gumonji.Network.Packets.Backd;

public sealed record UserOfflineRequest(uint UserId) : IIncomingPacket<UserOfflineRequest>
{
    public static UserOfflineRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var uid = r.ReadUInt32();
        r.ExpectEnd();
        return new(uid);
    }
}

namespace gumonji.Network.Packets.Backd;

public sealed record AllocateDoorIdsRequest(uint Count) : IIncomingPacket<AllocateDoorIdsRequest>
{
    public static AllocateDoorIdsRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var r = new PacketReader(data);
        var count = r.ReadUInt32();
        r.ExpectEnd();
        return new(count);
    }
}

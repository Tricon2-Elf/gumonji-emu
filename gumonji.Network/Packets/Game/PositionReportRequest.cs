namespace gumonji.Network.Packets.Game;

public sealed record PositionReportRequest(uint X, uint Y) : IIncomingPacket<PositionReportRequest>
{
    public static PositionReportRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 8)
            throw new InvalidDataException("invalid POSITION_REPORT size");
        var reader = new PacketReader(data);
        return new PositionReportRequest(reader.ReadUInt32(), reader.ReadUInt32());
    }
}

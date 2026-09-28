namespace gumonji.Network.Packets.Game;

public sealed class PositionReportRequest : IIncomingPacket<PositionReportRequest>
{
    public static PositionReportRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 8)
            throw new InvalidDataException("invalid POSITION_REPORT size");
        return new PositionReportRequest();
    }
}

namespace gumonji.Network.Packets.Zone;

public sealed class PeriodicReportRequest : IIncomingPacket<PeriodicReportRequest>
{
    public static PeriodicReportRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 8)
            throw new InvalidDataException("invalid PERIODIC_REPORT size");
        return new PeriodicReportRequest();
    }
}

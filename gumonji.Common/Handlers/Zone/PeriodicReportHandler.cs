namespace gumonji.Common.Handlers.Zone;

public sealed class PeriodicReportHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.PeriodicReportRequest;
    public ServerKind Server => ServerKind.Zone;

    public Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.State != SessionState.ZoneEntered)
            return Task.CompletedTask;
        _ = PeriodicReportRequest.FromBytes(payload.Span);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

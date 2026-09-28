namespace gumonji.Common.Handlers.Game;

public sealed class PeriodicReportHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.PeriodicReportRequest;
    public ServerKind Server => ServerKind.Game;

    public Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.State != SessionState.ZoneEntered)
            return Task.CompletedTask;
        _ = PeriodicReportRequest.FromBytes(payload.Span);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

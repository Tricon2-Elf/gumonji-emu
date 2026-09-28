namespace gumonji.Common.Handlers.Game;

public sealed class PositionReportHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.PositionReportRequest;
    public ServerKind Server => ServerKind.Game;

    public Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.State != SessionState.ZoneEntered)
            return Task.CompletedTask;
        _ = PositionReportRequest.FromBytes(payload.Span);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

namespace gumonji.Common.Handlers.Zone;

public sealed class PositionReportHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.PositionReportRequest;
    public ServerKind Server => ServerKind.Zone;

    public Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.State != SessionState.ZoneEntered)
            return Task.CompletedTask;
        var request = PositionReportRequest.FromBytes(payload.Span);
        session.PositionX = request.X;
        session.PositionY = request.Y;
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

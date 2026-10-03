namespace gumonji.Common.Handlers.Zone;

public sealed class PositionReportHandler : SessionPacketHandler<GumonjiSession>
{
    public override PacketType RequestType => PacketType.PositionReportRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.State != SessionState.ZoneEntered)
            return Task.CompletedTask;
        var request = PositionReportRequest.FromBytes(payload.Span);
        session.PositionX = request.X;
        session.PositionY = request.Y;
        if (session.CharacterId is { } id)
            session.Zone.Move(id, request.X, request.Y);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

namespace gumonji.Common.Handlers.Zone;

public sealed class MovementHandler : SessionPacketHandler<GumonjiSession>
{
    public override PacketType RequestType => PacketType.MovementRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.State != SessionState.ZoneEntered)
            return Task.CompletedTask;
        _ = MovementRequest.FromBytes(payload.Span);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

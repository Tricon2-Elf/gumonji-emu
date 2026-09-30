namespace gumonji.Common.Handlers.Zone;

public sealed class MovementHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MovementRequest;
    public ServerKind Server => ServerKind.Zone;

    public Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.State != SessionState.ZoneEntered)
            return Task.CompletedTask;
        _ = MovementRequest.FromBytes(payload.Span);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

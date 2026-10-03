namespace gumonji.Common.Handlers.Zone;

public sealed class ChunkLoadedHandler : SessionPacketHandler<GumonjiSession>
{
    public override PacketType RequestType => PacketType.ChunkLoadedRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.State != SessionState.ZoneEntered)
            return Task.CompletedTask;
        _ = ChunkLoadedRequest.FromBytes(payload.Span);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

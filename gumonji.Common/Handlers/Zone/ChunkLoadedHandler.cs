namespace gumonji.Common.Handlers.Zone;

public sealed class ChunkLoadedHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.ChunkLoadedRequest;
    public ServerKind Server => ServerKind.Zone;

    public Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.State != SessionState.ZoneEntered)
            return Task.CompletedTask;
        _ = ChunkLoadedRequest.FromBytes(payload.Span);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

namespace gumonji.Common.Handlers.Femsg;

public sealed class HeartbeatHandler : PacketHandlerBase<HeartbeatRequest>
{
    public override PacketType RequestType => PacketType.HeartbeatRequest;
    public override ServerKind Server => ServerKind.Femsg;

    public override Task HandleAsync(HeartbeatRequest request, GumonjiSession session, CancellationToken ct) =>
        session.SendAsync(HeartbeatReply.FromRequest(request), ct);
}

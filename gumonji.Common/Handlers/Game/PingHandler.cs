namespace gumonji.Common.Handlers.Game;

public sealed class PingHandler : PacketHandlerBase<PingRequest>
{
    public override PacketType RequestType => PacketType.PingRequest;
    public override ServerKind Server => ServerKind.Game;

    public override Task HandleAsync(PingRequest request, GumonjiSession session, CancellationToken ct) =>
        session.SendAsync(new PingResponse(request.Echoed), ct);
}

namespace gumonji.Common.Handlers.Game;

public sealed class PingHandler : PacketHandlerBase<PingRequest>
{
    public override PacketType RequestType => PacketType.PingRequest;
    public override ServerKind Server => ServerKind.Game;

    public override async Task HandleAsync(PingRequest request, GumonjiSession session, CancellationToken ct)
    {
        if (session.State == SessionState.ZoneEntered)
            await session.SaveConditionAsync(ct: ct);
        await session.SendAsync(new PingResponse(request.Echoed), ct);
    }
}

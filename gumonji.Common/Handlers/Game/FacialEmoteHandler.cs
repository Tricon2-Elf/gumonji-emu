namespace gumonji.Common.Handlers.Game;

public sealed class FacialEmoteHandler : PacketHandlerBase<FacialEmoteRequest>
{
    public override PacketType RequestType => PacketType.FacialEmoteRequest;
    public override ServerKind Server => ServerKind.Game;

    public override Task HandleAsync(FacialEmoteRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        session.LastFacialEmoteId = request.EmotionId;
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

namespace gumonji.Common.Handlers.Game;

public sealed class ActionEmoteHandler : PacketHandlerBase<ActionEmoteRequest>
{
    public override PacketType RequestType => PacketType.ActionEmoteRequest;
    public override ServerKind Server => ServerKind.Game;

    public override Task HandleAsync(ActionEmoteRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        session.LastActionEmote = new ActionEmoteState(request.ActionId, request.Sequence);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

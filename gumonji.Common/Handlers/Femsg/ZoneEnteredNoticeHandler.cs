namespace gumonji.Common.Handlers.Femsg;

public sealed class ZoneEnteredNoticeHandler : PacketHandlerBase<ZoneEnteredNotice>
{
    public override PacketType RequestType => PacketType.ZoneEnteredNotice;
    public override ServerKind Server => ServerKind.Femsg;

    public override Task HandleAsync(ZoneEnteredNotice request, GumonjiSession session, CancellationToken ct)
    {
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

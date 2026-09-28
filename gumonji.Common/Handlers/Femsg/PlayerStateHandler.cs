namespace gumonji.Common.Handlers.Femsg;

public sealed class PlayerStateHandler : PacketHandlerBase<PlayerStateNotice>
{
    public override PacketType RequestType => PacketType.PlayerStateNotice;
    public override ServerKind Server => ServerKind.Femsg;

    public override Task HandleAsync(PlayerStateNotice request, GumonjiSession session, CancellationToken ct)
    {
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

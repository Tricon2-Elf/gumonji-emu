namespace gumonji.Common.Handlers.Zone;

public sealed class NoOp2082Handler : PacketHandlerBase<NoOp2082Request>
{
    public override PacketType RequestType => PacketType.NoOp2082Request;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(NoOp2082Request request, GumonjiSession session, CancellationToken ct)
    {
        // sub_4557B0 returns zero without checking session state or replying.
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

namespace gumonji.Common.Handlers.Zone;

public sealed class NoOp1B62Handler : PacketHandlerBase<NoOp1B62Request>
{
    public override PacketType RequestType => PacketType.NoOp1B62Request;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(NoOp1B62Request request, GumonjiSession session, CancellationToken ct)
    {
        // sub_4547C0 returns zero without checking session state or replying.
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

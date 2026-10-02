namespace gumonji.Common.Handlers.Zone;

public sealed class NoOp1FC2Handler : PacketHandlerBase<NoOp1FC2Request>
{
    public override PacketType RequestType => PacketType.NoOp1FC2Request;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(NoOp1FC2Request request, GumonjiSession session, CancellationToken ct)
    {
        // sub_455930 returns zero without checking session state or replying.
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

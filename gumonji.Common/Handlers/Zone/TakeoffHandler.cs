namespace gumonji.Common.Handlers.Zone;

public sealed class TakeoffHandler : PacketHandlerBase<TakeoffRequest>
{
    public override PacketType RequestType => PacketType.TakeoffRequest;
    public override ServerKind Server => ServerKind.Zone;
    public override Task HandleAsync(TakeoffRequest request, GumonjiSession session, CancellationToken ct)
    {
        // Original sub_44E890 only logs. It does not change equipment.
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

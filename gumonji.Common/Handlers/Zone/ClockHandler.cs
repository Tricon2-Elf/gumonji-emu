namespace gumonji.Common.Handlers.Zone;

public sealed class ClockHandler : PacketHandlerBase<ClockRequest>
{
    public override PacketType RequestType => PacketType.ClockRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(ClockRequest request, GumonjiSession session, CancellationToken ct)
    {
        var (day, hour, minute) = session.GameClock();
        return session.SendAsync(new ClockSyncResponse(day, hour, minute), ct);
    }
}

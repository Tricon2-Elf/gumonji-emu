namespace gumonji.Common.Handlers.Zone;

public sealed class MovementTotalsHandler : PacketHandlerBase<MovementTotalsRequest>
{
    public override PacketType RequestType => PacketType.MovementTotalsRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override async Task HandleAsync(MovementTotalsRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        // Use authenticated ownership, never the opaque client character field.
        await session.SaveConditionAsync(request.Walking, request.Swimming, ct);
        session.SilentNoReply = true; // notification with persisted effects, not a request for a dialog
    }
}

namespace gumonji.Common.Handlers.Zone;

public sealed class EntityPositionHandler : PacketHandlerBase<EntityPositionRequest>
{
    public override PacketType RequestType => PacketType.EntityPositionRequest;
    public override ServerKind Server => ServerKind.Zone;
    public override Task HandleAsync(EntityPositionRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        // sub_42BBA0 checks the original character registry, not the item/animal registries.
        var entity = session.Zone.Find(request.EntityId);
        if (entity?.Kind != World.ZoneEntityKind.Player) entity = null;
        return session.SendAsync(new EntityPositionResponse(entity is not null ? 0u : uint.MaxValue,
            request.EntityId, entity?.X ?? 0, entity?.Y ?? 0), ct);
    }
}

namespace gumonji.Common.Handlers.Zone;

public sealed class NearestCharacterHandler : PacketHandlerBase<NearestCharacterRequest>
{
    public override PacketType RequestType => PacketType.NearestCharacterRequest;
    public override ServerKind Server => ServerKind.Zone;
    public override Task HandleAsync(NearestCharacterRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        var nearest = session.Zone.Snapshot().Where(e => e.Kind == World.ZoneEntityKind.Player &&
            e.Id != session.CharacterId && session.Zone.FindPlayer(e.Id) is not null)
            .Select(e => (Entity: e, Distance: Distance(e, session.PositionX, session.PositionY)))
            .Where(e => e.Distance < int.MaxValue)
            .OrderBy(e => e.Distance)
            .Select(e => e.Entity)
            .FirstOrDefault();
        var player = nearest is null ? null : session.Zone.FindPlayer(nearest.Id);
        // sub_42B980 excludes the requester; reply contains account id, not entity id.
        return session.SendAsync(new NearestCharacterResponse(player is null ? unchecked((uint)-27) : 0,
            player?.UserId ?? 0), ct);
    }

    private static int Distance(World.ZoneEntity entity, uint x, uint y)
    {
        // Original sub_494DF0 narrows squared distance to float, calls sqrt through
        // sub_408AA0, then converts to integer. Keep its integer-distance ties.
        // Confirmed against instructions at 00494DF0..00494E3C (IDA loses sqrt's return).
        var dx = unchecked((int)(entity.X - x));
        var dy = unchecked((int)(entity.Y - y));
        return unchecked((int)(long)Math.Sqrt((float)((double)dx * dx + (double)dy * dy)));
    }
}

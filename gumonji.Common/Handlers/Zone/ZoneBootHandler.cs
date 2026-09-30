namespace gumonji.Common.Handlers.Zone;

public sealed class ZoneBootHandler : PacketHandlerBase<ZoneBootRequest>
{
    public override PacketType RequestType => PacketType.ZoneBootRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(ZoneBootRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        return session.SendAsync(new ZoneBootAckResponse(), ct);
    }
}

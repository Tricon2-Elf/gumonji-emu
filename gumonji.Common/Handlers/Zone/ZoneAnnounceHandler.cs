namespace gumonji.Common.Handlers.Zone;

public sealed class ZoneAnnounceHandler : PacketHandlerBase<ZoneAnnounceRequest>
{
    public override PacketType RequestType => PacketType.ZoneAnnounceRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(ZoneAnnounceRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        return Task.CompletedTask;
    }
}

namespace gumonji.Common.Handlers.Game;

public sealed class ZoneAnnounceHandler : PacketHandlerBase<ZoneAnnounceRequest>
{
    public override PacketType RequestType => PacketType.ZoneAnnounceRequest;
    public override ServerKind Server => ServerKind.Game;

    public override Task HandleAsync(ZoneAnnounceRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        return Task.CompletedTask;
    }
}

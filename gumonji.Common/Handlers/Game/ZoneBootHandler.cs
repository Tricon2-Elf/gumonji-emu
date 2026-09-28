namespace gumonji.Common.Handlers.Game;

public sealed class ZoneBootHandler : PacketHandlerBase<ZoneBootRequest>
{
    public override PacketType RequestType => PacketType.ZoneBootRequest;
    public override ServerKind Server => ServerKind.Game;

    public override Task HandleAsync(ZoneBootRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        return session.SendAsync(new ZoneBootAckResponse(), ct);
    }
}

namespace gumonji.Common.Handlers.Zone;

public sealed class StringListHandler : PacketHandlerBase<StringListRequest>
{
    public override PacketType RequestType => PacketType.StringListRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(StringListRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        return session.SendAsync(new StringListResponse(), ct);
    }
}

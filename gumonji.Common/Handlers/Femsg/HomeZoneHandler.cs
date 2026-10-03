namespace gumonji.Common.Handlers.Femsg;

public sealed class HomeZoneHandler : SessionPacketHandler<GumonjiSession>
{
    public override PacketType RequestType => PacketType.HomeZoneRequest;
    public override ServerKind Server => ServerKind.Femsg;

    public override Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null)
            throw new InvalidDataException("zone request before frontend login");
        _ = HomeZoneRequest.FromBytes(payload.Span);
        return session.SendAsync(new HomeZoneReply((uint)session.Options.HomeZone), ct);
    }
}

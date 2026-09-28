namespace gumonji.Common.Handlers.Femsg;

public sealed class HomeZoneHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.HomeZoneRequest;
    public ServerKind Server => ServerKind.Femsg;

    public Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null)
            throw new InvalidDataException("zone request before frontend login");
        _ = HomeZoneRequest.FromBytes(payload.Span);
        return session.SendAsync(new HomeZoneReply((uint)session.Options.HomeZone), ct);
    }
}

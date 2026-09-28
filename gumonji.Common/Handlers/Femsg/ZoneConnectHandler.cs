namespace gumonji.Common.Handlers.Femsg;

public sealed class ZoneConnectHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.ZoneConnectRequest;
    public ServerKind Server => ServerKind.Femsg;

    public Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null)
            throw new InvalidDataException("zone request before frontend login");
        _ = ZoneConnectRequest.FromBytes(payload.Span);
        var token = session.Accounts.Issue(session.UserId.Value, session.Options.Otp);
        session.State = SessionState.HandoffIssued;
        return session.SendAsync(
            new ZoneHandoffResponse(token, session.Options.AdvertiseIp, (ushort)session.Options.GamePort),
            ct);
    }
}

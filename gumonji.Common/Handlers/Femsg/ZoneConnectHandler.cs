namespace gumonji.Common.Handlers.Femsg;

public sealed class ZoneConnectHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.ZoneConnectRequest;
    public ServerKind Server => ServerKind.Femsg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null)
            throw new InvalidDataException("zone request before frontend login");
        _ = ZoneConnectRequest.FromBytes(payload.Span);
        var token = await session.Accounts.IssueAsync(session.UserId.Value, session.Options.Otp, ct);
        session.State = SessionState.HandoffIssued;
        await session.SendAsync(
            new ZoneHandoffResponse(token, session.Options.AdvertiseIp, (ushort)session.Options.ZonePort),
            ct);
    }
}

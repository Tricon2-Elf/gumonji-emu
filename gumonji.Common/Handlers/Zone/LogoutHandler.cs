namespace gumonji.Common.Handlers.Zone;

public sealed class LogoutHandler : PacketHandlerBase<LogoutRequest>
{
    public override PacketType RequestType => PacketType.LogoutRequest;
    public override ServerKind Server => ServerKind.Zone;
    public override async Task HandleAsync(LogoutRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.SilentNoReply = true;
        await session.DisconnectAsync(ct);
    }
}

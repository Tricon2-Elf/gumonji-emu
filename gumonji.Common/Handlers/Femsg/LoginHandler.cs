namespace gumonji.Common.Handlers.Femsg;

public sealed class LoginHandler : PacketHandlerBase<LoginRequest>
{
    public override PacketType RequestType => PacketType.LoginRequest;
    public override ServerKind Server => ServerKind.Femsg;

    public override Task HandleAsync(LoginRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.UserId = session.Accounts.Login(request.Username);
        session.State = SessionState.WaitZoneRequest;
        return session.SendAsync(new LoginAcceptResponse(session.UserId.Value), ct);
    }
}

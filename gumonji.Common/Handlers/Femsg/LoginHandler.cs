namespace gumonji.Common.Handlers.Femsg;

public sealed class LoginHandler : PacketHandlerBase<LoginRequest>
{
    public override PacketType RequestType => PacketType.LoginRequest;
    public override ServerKind Server => ServerKind.Femsg;

    public override async Task HandleAsync(LoginRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.UserId = await session.Accounts.LoginAsync(request.Username, request.Password, ct);
        session.State = SessionState.WaitZoneRequest;
        var tutorialFlags = await session.Accounts.GetTutorialFlagsAsync(session.UserId.Value, ct);
        await session.SendAsync(new LoginAcceptResponse(session.UserId.Value, tutorialFlags), ct);
    }
}

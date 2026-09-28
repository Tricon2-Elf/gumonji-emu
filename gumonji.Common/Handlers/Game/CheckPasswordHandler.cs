namespace gumonji.Common.Handlers.Game;

public sealed class CheckPasswordHandler : PacketHandlerBase<CheckPasswordRequest>
{
    public override PacketType RequestType => PacketType.CheckPasswordRequest;
    public override ServerKind Server => ServerKind.Game;

    public override Task HandleAsync(CheckPasswordRequest request, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is not null)
            throw new InvalidDataException("duplicate CHECK_PASSWORD on authenticated connection");
        if (!session.Accounts.Consume(request.UserId, request.Token))
            throw new InvalidDataException($"CHECK_PASSWORD rejected uid={request.UserId}: invalid/expired/used token");
        session.UserId = request.UserId;
        session.State = SessionState.WaitCharacterCheck;
        return session.SendAsync(new CheckPasswordAcceptResponse(request.UserId), ct);
    }
}

namespace gumonji.Common.Handlers.Zone;

public sealed class CheckPasswordHandler : PacketHandlerBase<CheckPasswordRequest>
{
    public override PacketType RequestType => PacketType.CheckPasswordRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override async Task HandleAsync(CheckPasswordRequest request, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is not null)
            throw new InvalidDataException("duplicate CHECK_PASSWORD on authenticated connection");
        if (!await session.LoginTokens.ConsumeAsync(request.UserId, request.Token, ct))
            throw new InvalidDataException($"CHECK_PASSWORD rejected uid={request.UserId}: invalid/expired/used token");
        session.UserId = request.UserId;
        session.State = SessionState.WaitCharacterCheck;
        await session.SendAsync(new CheckPasswordAcceptResponse(request.UserId), ct);
    }
}

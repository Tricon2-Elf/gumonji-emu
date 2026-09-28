using gumonji.Common.Accounts;

namespace gumonji.Common.Handlers.Game;

public sealed class CharacterCreateHandler : PacketHandlerBase<CharacterCreateRequest>
{
    public override PacketType RequestType => PacketType.CharacterCreateRequest;
    public override ServerKind Server => ServerKind.Game;

    public override async Task HandleAsync(CharacterCreateRequest request, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null)
            throw new InvalidDataException("CHARACTER_CREATE before game authentication");
        session.Accounts.Characters[session.UserId.Value] = new Character(
            request.Name, (int)request.Body, (int)request.Model, (int)request.Style, (int)request.Color);
        session.State = SessionState.CharacterCreated;
        await session.SendAsync(new CharacterCreateAcceptResponse(), ct);
        await session.SendAsync(new CharacterAssignResponse(session.UserId.Value), ct);
    }
}

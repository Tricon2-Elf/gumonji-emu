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
        await session.Accounts.SaveCharacterAsync(
            session.UserId.Value,
            new DAL.Entities.Character
            {
                Name = request.Name,
                Body = (int)request.Body,
                Model = (int)request.Model,
                Style = (int)request.Style,
                Color = (int)request.Color,
            },
            ct);
        session.State = SessionState.CharacterCreated;
        await session.SendAsync(new CharacterCreateAcceptResponse(), ct);
        await session.SendAsync(new CharacterAssignResponse(session.UserId.Value), ct);
    }
}

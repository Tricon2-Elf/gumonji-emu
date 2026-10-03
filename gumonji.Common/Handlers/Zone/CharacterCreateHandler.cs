using gumonji.Common.Accounts;

namespace gumonji.Common.Handlers.Zone;

public sealed class CharacterCreateHandler : PacketHandlerBase<CharacterCreateRequest>
{
    public override PacketType RequestType => PacketType.CharacterCreateRequest;
    public override ServerKind Server => ServerKind.Zone;

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
        var character = await session.Accounts.GetCharacterAsync(session.UserId.Value, ct)
            ?? throw new InvalidDataException("CHARACTER_CREATE was not persisted");
        var entityId = session.AssignCharacter(character.Id);
        session.State = SessionState.CharacterCreated;
        await session.SendAsync(new CharacterCreateAcceptResponse(), ct);
        await session.SendAsync(new CharacterAssignResponse(entityId), ct);
    }
}

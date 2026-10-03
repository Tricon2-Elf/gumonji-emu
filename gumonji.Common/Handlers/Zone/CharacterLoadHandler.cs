namespace gumonji.Common.Handlers.Zone;

public sealed class CharacterLoadHandler : PacketHandlerBase<CharacterLoadRequest>
{
    public override PacketType RequestType => PacketType.CharacterLoadRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override async Task HandleAsync(CharacterLoadRequest request, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null || session.State != SessionState.WaitCharacterLoad)
            throw new InvalidDataException("CHARACTER_LOAD before a saved character was offered");

        var character = await session.Accounts.GetCharacterAsync(session.UserId.Value, ct);
        if (character is null)
            throw new InvalidDataException("CHARACTER_LOAD requested without a saved character");

        var entityId = session.AssignCharacter(character.Id);
        session.State = SessionState.CharacterCreated;
        await session.SendAsync(new CharacterAssignResponse(entityId), ct);
    }
}

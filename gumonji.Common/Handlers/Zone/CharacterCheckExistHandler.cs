namespace gumonji.Common.Handlers.Zone;

public sealed class CharacterCheckExistHandler : SessionPacketHandler<GumonjiSession>
{
    public override PacketType RequestType => PacketType.CharacterCheckExistRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override async Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null || session.State != SessionState.WaitCharacterCheck || payload.Length != 0)
            throw new InvalidDataException("invalid or unauthenticated CHARACTER_CHECK_EXIST");
        var exists = await session.Characters.GetByUserIdAsync(session.UserId.Value, ct) is not null;
        session.State = exists ? SessionState.WaitCharacterLoad : SessionState.CharacterCreationMenu;
        await session.SendAsync(new CharacterCheckExistResponse(exists), ct);
    }
}

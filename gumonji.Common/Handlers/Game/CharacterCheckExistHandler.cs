namespace gumonji.Common.Handlers.Game;

public sealed class CharacterCheckExistHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.CharacterCheckExistRequest;
    public ServerKind Server => ServerKind.Game;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null || session.State != SessionState.WaitCharacterCheck || payload.Length != 0)
            throw new InvalidDataException("invalid or unauthenticated CHARACTER_CHECK_EXIST");
        var exists = await session.Accounts.GetCharacterAsync(session.UserId.Value, ct) is not null;
        session.State = exists ? SessionState.WaitCharacterLoad : SessionState.CharacterCreationMenu;
        await session.SendAsync(new CharacterCheckExistResponse(exists), ct);
    }
}

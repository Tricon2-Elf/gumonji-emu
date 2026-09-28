namespace gumonji.Common.Handlers.Game;

public sealed class CharacterCheckExistHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.CharacterCheckExistRequest;
    public ServerKind Server => ServerKind.Game;

    public Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null || payload.Length != 0)
            throw new InvalidDataException("invalid or unauthenticated CHARACTER_CHECK_EXIST");
        session.State = SessionState.CharacterCreationMenu;
        return session.SendAsync(new CharacterNotFoundResponse(), ct);
    }
}

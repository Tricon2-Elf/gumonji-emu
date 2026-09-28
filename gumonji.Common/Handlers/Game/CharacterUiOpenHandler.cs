namespace gumonji.Common.Handlers.Game;

public sealed class CharacterUiOpenHandler : PacketHandlerBase<CharacterUiOpenRequest>
{
    public override PacketType RequestType => PacketType.CharacterUiOpenRequest;
    public override ServerKind Server => ServerKind.Game;

    public override Task HandleAsync(CharacterUiOpenRequest request, GumonjiSession session, CancellationToken ct) =>
        session.SendAsync(new CharacterUiAckResponse(), ct);
}

namespace gumonji.Common.Handlers.Zone;

public sealed class CharacterUiOpenHandler : PacketHandlerBase<CharacterUiOpenRequest>
{
    public override PacketType RequestType => PacketType.CharacterUiOpenRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(CharacterUiOpenRequest request, GumonjiSession session, CancellationToken ct) =>
        session.SendAsync(new CharacterUiAckResponse(), ct);
}

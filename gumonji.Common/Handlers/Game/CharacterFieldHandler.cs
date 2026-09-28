namespace gumonji.Common.Handlers.Game;

public sealed class CharacterFieldHandler : PacketHandlerBase<CharacterFieldRequest>
{
    public override PacketType RequestType => PacketType.CharacterFieldRequest;
    public override ServerKind Server => ServerKind.Game;

    public override Task HandleAsync(CharacterFieldRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        return session.SendAsync(new CharacterFieldAckResponse(), ct);
    }
}

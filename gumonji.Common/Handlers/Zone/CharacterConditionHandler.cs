namespace gumonji.Common.Handlers.Zone;

public sealed class CharacterConditionHandler : PacketHandlerBase<CharacterConditionRequest>
{
    public override PacketType RequestType => PacketType.CharacterConditionRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override async Task HandleAsync(CharacterConditionRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        await session.SaveConditionAsync(ct: ct);
        var character = await session.Characters.GetByUserIdAsync(session.UserId!.Value, ct)
            ?? throw new InvalidDataException("condition requested without a saved character");
        await session.SendAsync(new CharacterConditionResponse(character.Name, WireTotal(character.PlayedSeconds),
            WireTotal(character.WalkingDistance), WireTotal(character.SwimmingDistance)), ct);
    }

    private static uint WireTotal(long value) => (uint)Math.Clamp(value, 0, uint.MaxValue);
}

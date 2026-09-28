using gumonji.Common.Accounts;

namespace gumonji.Common.Handlers.Game;

public sealed class ZoneEnterHandler : PacketHandlerBase<ZoneEnterRequest>
{
    public override PacketType RequestType => PacketType.ZoneEnterRequest;
    public override ServerKind Server => ServerKind.Game;

    public override async Task HandleAsync(ZoneEnterRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        var userId = session.UserId!.Value;
        if (!session.Accounts.Characters.TryGetValue(userId, out var character))
            character = new Character("Local Player"u8.ToArray(), 0, 0, 0, 0);
        session.State = SessionState.ZoneEntered;
        await session.SendAsync(new ZoneEnterResponse(64, 64), ct);
        await session.SendAsync(new EntityPlaceResponse(userId, 64, 64), ct);
        await session.SendAsync(
            new CharacterAvatarResponse(userId, character.Name, character.Body, character.Model, character.Style, character.Color),
            ct);
    }
}

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
        var character = await session.Accounts.GetCharacterAsync(userId, ct)
            ?? new DAL.Entities.Character { Name = "Local Player"u8.ToArray() };
        var characterId = session.CharacterId ?? checked((uint)character.Id);
        if (characterId == 0)
            throw new InvalidDataException("ZONE_ENTER before character assignment");
        session.CharacterId = characterId;
        session.State = SessionState.ZoneEntered;
        session.PositionX = 64000;
        session.PositionY = 64000;
        session.StartPlayTime();
        await session.SendAsync(new ZoneEnterResponse(64, 64), ct);
        await session.SendAsync(new EntityPlaceResponse(characterId, 64, 64), ct);
        await session.SendAsync(
            new CharacterAvatarResponse(characterId, character.Name, character.Body, character.Model, character.Style, character.Color),
            ct);
        if (character.Id != 0)
            await session.Accounts.Gameplay.EnsureStarterVehicleAsync(userId, ct);
        foreach (var item in await session.Accounts.Gameplay.GetInventoryAsync(userId, ct))
            await session.SendAsync(PlantHarvestHandler.InventoryPacket(item), ct);
    }
}

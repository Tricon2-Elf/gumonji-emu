
namespace gumonji.Common.Handlers.Zone;

public sealed class ZoneEnterHandler : PacketHandlerBase<ZoneEnterRequest>
{
    public override PacketType RequestType => PacketType.ZoneEnterRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override async Task HandleAsync(ZoneEnterRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        var userId = session.UserId!.Value;
        var character = await session.Characters.GetByUserIdAsync(userId, ct)
            ?? new DAL.Entities.Character { Name = "Local Player"u8.ToArray() };
        var characterId = session.CharacterId ?? checked((uint)character.Id);
        if (characterId == 0)
            throw new InvalidDataException("ZONE_ENTER before character assignment");
        session.CharacterId = characterId;
        session.State = SessionState.ZoneEntered;
        session.PositionX = 64000;
        session.PositionY = 64000;
        session.StartPlayTime();
        var registeredId = session.Zone.Register(World.ZoneEntityKind.Player, character.Id,
            session.PositionX, session.PositionY, characterId);
        if (registeredId != characterId)
            throw new InvalidDataException("zone entry character id differs from assignment");
        session.Zone.Attach(session);
        await session.SendAsync(new ZoneEnterResponse(64, 64), ct);
        await session.SendAsync(new EntityPlaceResponse(characterId, 64, 64), ct);
        await session.SendAsync(
            new CharacterAvatarResponse(characterId, character.Name, character.Body, character.Model, character.Style, character.Color),
            ct);
        if (character.Id != 0)
            await session.Gameplay.EnsureStarterVehicleAsync(userId, ct);
        foreach (var item in await session.Gameplay.GetInventoryAsync(userId, ct))
            await session.SendAsync(PlantHarvestHandler.InventoryPacket(item), ct);
    }
}

using gumonji.Common.World;
using gumonji.Network.Packets.Zone;

namespace gumonji.Common.Handlers.Zone;

public sealed class ItemPickupHandler : PacketHandlerBase<ItemPickupRequest>
{
    public override PacketType RequestType => PacketType.ItemPickupRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override async Task HandleAsync(ItemPickupRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        if (session.State != SessionState.ZoneEntered || session.UserId is null || session.CharacterId is null)
            throw new InvalidDataException("item pickup before zone entry");

        var dx = (double)session.PositionX - SpawnActors.CarX * 1000;
        var dy = (double)session.PositionY - SpawnActors.CarY * 1000;
        if (request.ItemId != SpawnActors.CarId || session.WorldVehiclePickedUp ||
            dx * dx + dy * dy > 4000.0 * 4000.0)
        {
            await session.SendAsync(new ItemPickupResponse(request.ItemId, false), ct);
            return;
        }

        var vehicle = await session.Accounts.Gameplay.EnsureStarterVehicleAsync(session.UserId.Value, ct);
        if (vehicle is null)
        {
            await session.SendAsync(new ItemPickupResponse(request.ItemId, false), ct);
            return;
        }

        session.EquippedVehicleId = checked((uint)vehicle.Id);
        session.WorldVehiclePickedUp = true;
        await session.SendAsync(new ItemPickupResponse(request.ItemId, true), ct);
        await session.SendAsync(new ItemRemoveResponse(request.ItemId), ct);
        await session.SendAsync(PlantHarvestHandler.InventoryPacket(vehicle), ct);
        var character = await session.Accounts.GetCharacterAsync(session.UserId.Value, ct)
            ?? throw new InvalidDataException("item pickup without a saved character");
        await session.SendAsync(new CharacterAvatarResponse(session.CharacterId.Value, character.Name,
            character.Body, character.Model, character.Style, character.Color,
            checked((byte)vehicle.Slot), session.EquippedVehicleId.Value), ct);
    }
}

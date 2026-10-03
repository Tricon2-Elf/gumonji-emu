using gumonji.Network.Packets.Zone;
using gumonji.Common.World;

namespace gumonji.Common.Handlers.Zone;

public sealed class ItemUseHandler : PacketHandlerBase<ItemUseRequest>
{
    public override PacketType RequestType => PacketType.ItemUseRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override async Task HandleAsync(ItemUseRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        if (session.State != SessionState.ZoneEntered || session.CharacterId is null || session.UserId is null)
            throw new InvalidDataException("item use before zone entry");
        var item = (await session.Gameplay.GetInventoryAsync(session.UserId.Value, ct))
            .SingleOrDefault(i => i.Slot == request.Slot);
        if (item is { ItemType: TreeSeeds.ItemType } && TreeSeeds.IsSupported(item.Subtype, item.Color))
        {
            var result = await session.Gameplay.PlantSeedAsync(session.UserId.Value,
                session.Options.HomeZone, request.Slot, request.X, request.Y,
                session.PositionX, session.PositionY, ct);
            await session.SendAsync(new ItemUseResponse(request.Slot, result.Plant is not null), ct);
            if (result.Plant is { } plant)
            {
                await session.SendAsync(new InventorySlotResponse(checked((byte)request.Slot),
                    0, 0, 0, 0, 0), ct);
                await session.SendAsync(new PlantPlaceResponse(
                    checked((uint)plant.Id),
                    checked((byte)plant.Subtype), checked((byte)plant.Color),
                    checked((uint)plant.Fertility), checked((ushort)plant.X), checked((ushort)plant.Y),
                    checked((byte)plant.Stage)), ct);
            }
            return;
        }
        if (item is not { ItemType: ItemTemplateIds.ToyCar })
        {
            await session.SendAsync(new ItemUseResponse(request.Slot, false), ct);
            return;
        }

        session.EquippedVehicleId = session.EquippedVehicleId == (uint)item.Id ? null : (uint)item.Id;
        await session.SendAsync(new ItemUseResponse(request.Slot, true), ct);
        var character = await session.Characters.GetByUserIdAsync(session.UserId.Value, ct)
            ?? throw new InvalidDataException("item use without a saved character");
        await session.SendAsync(new CharacterAvatarResponse(session.CharacterId.Value, character.Name,
            character.Body, character.Model, character.Style, character.Color,
            session.EquippedVehicleId is not null ? checked((byte)item.Slot) : null,
            session.EquippedVehicleId ?? 0), ct);
    }
}

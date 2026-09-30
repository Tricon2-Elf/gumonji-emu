using gumonji.Network.Packets.Game;

namespace gumonji.Common.Handlers.Game;

public sealed class ItemUseHandler : PacketHandlerBase<ItemUseRequest>
{
    public override PacketType RequestType => PacketType.ItemUseRequest;
    public override ServerKind Server => ServerKind.Game;

    public override async Task HandleAsync(ItemUseRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        if (session.State != SessionState.ZoneEntered || session.CharacterId is null || session.UserId is null)
            throw new InvalidDataException("item use before zone entry");
        var vehicle = (await session.Accounts.Gameplay.GetInventoryAsync(session.UserId.Value, ct))
            .SingleOrDefault(i => i.Slot == request.Slot && i.ItemType == ItemTemplateIds.ToyCar);
        if (vehicle is null)
        {
            await session.SendAsync(new ItemUseResponse(request.Slot, false), ct);
            return;
        }

        session.EquippedVehicleId = session.EquippedVehicleId == (uint)vehicle.Id ? null : (uint)vehicle.Id;
        await session.SendAsync(new ItemUseResponse(request.Slot, true), ct);
        var character = await session.Accounts.GetCharacterAsync(session.UserId.Value, ct)
            ?? throw new InvalidDataException("item use without a saved character");
        await session.SendAsync(new CharacterAvatarResponse(session.CharacterId.Value, character.Name,
            character.Body, character.Model, character.Style, character.Color,
            session.EquippedVehicleId is not null ? checked((byte)vehicle.Slot) : null,
            session.EquippedVehicleId ?? 0), ct);
    }
}

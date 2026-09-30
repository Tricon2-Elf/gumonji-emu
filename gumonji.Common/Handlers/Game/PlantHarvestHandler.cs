using System.Text;
using gumonji.Common.DAL.Entities;
using gumonji.Common.World;

namespace gumonji.Common.Handlers.Game;

public sealed class PlantHarvestHandler : PacketHandlerBase<PlantHarvestRequest>
{
    public override PacketType RequestType => PacketType.PlantHarvestRequest;
    public override ServerKind Server => ServerKind.Game;

    public override async Task HandleAsync(PlantHarvestRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        if (session.State != SessionState.ZoneEntered || session.CharacterId is null)
            throw new InvalidDataException("harvest before zone entry");
        var result = await session.Accounts.Gameplay.HarvestAsync(session.UserId!.Value, session.Options.HomeZone,
            request.PlantId, session.PositionX, session.PositionY, ct);
        if (result.Item is not null)
            await session.SendAsync(InventoryPacket(result.Item), ct);
        if (result.Plant is not null)
        {
            await session.SendAsync(new PlantPlaceResponse(checked((uint)result.Plant.Id),
                checked((byte)result.Plant.Subtype), checked((byte)result.Plant.Color),
                checked((uint)result.Plant.Fertility), checked((ushort)result.Plant.X),
                checked((ushort)result.Plant.Y), checked((byte)result.Plant.Stage)), ct);
        }
        // Also explain failed attempts; do not silently swallow full/out-of-range requests.
        await session.SendAsync(new ChatEventResponse(session.CharacterId.Value, "Harvest"u8.ToArray(),
            Encoding.ASCII.GetBytes(result.Error ?? "Harvested a bamboo tree seed.")), ct);
    }

    internal static InventorySlotResponse InventoryPacket(InventoryItem item) => new(
        checked((byte)item.Slot), checked((uint)item.Id), checked((ushort)item.ItemType),
        checked((byte)item.Subtype), checked((byte)item.Color), checked((uint)item.Fertility));
}

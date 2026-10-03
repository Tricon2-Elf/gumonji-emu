namespace gumonji.Common.Handlers.Zone;

public sealed class RenderTaskHandler : PacketHandlerBase<RenderTaskRequest>
{
    public override PacketType RequestType => PacketType.RenderTaskRequest;
    public override ServerKind Server => ServerKind.Zone;
    public override async Task HandleAsync(RenderTaskRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        var character = await session.Accounts.GetCharacterAsync(session.UserId!.Value, ct)
            ?? throw new InvalidDataException("render query without a saved character");
        var items = new List<RenderItem>();
        if (session.EquippedVehicleId is { } id)
        {
            var item = (await session.Accounts.Gameplay.GetInventoryAsync(session.UserId.Value, ct))
                .SingleOrDefault(i => (uint)i.Id == id);
            if (item is not null)
                items.Add(new(checked((byte)item.Slot), checked((ushort)item.ItemType),
                    checked((byte)item.Subtype), checked((byte)item.Color)));
        }
        await session.SendAsync(new RenderTaskResponse(session.UserId.Value, character.Body,
            character.Model, character.Style, 0, items), ct);
    }
}

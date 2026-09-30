using gumonji.Common.World;

namespace gumonji.Common.Handlers.Zone;

public sealed class GetPageDataHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.GetPageDataRequest;
    public ServerKind Server => ServerKind.Zone;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        if (session.State != SessionState.ZoneEntered || payload.Length != 8)
            throw new InvalidDataException("GET_PAGE_DATA before zone entry or truncated");
        var request = GetPageDataRequest.FromBytes(payload.Span);
        await session.SendAsync(TerrainWorld.CreatePage(request.ChunkX, request.ChunkY), ct);
        if (!session.PlantedChunks.Add((request.ChunkX, request.ChunkY)))
            return;
        var savedPlants = await session.Accounts.Gameplay.GetPlantsInChunkAsync(
            session.Options.HomeZone, request.ChunkX, request.ChunkY, ct);
        foreach (var plant in savedPlants)
            await session.SendAsync(new PlantPlaceResponse(
                checked((uint)plant.Id),
                checked((byte)plant.Subtype), checked((byte)plant.Color),
                checked((uint)plant.Fertility), checked((ushort)plant.X), checked((ushort)plant.Y),
                checked((byte)plant.Stage)), ct);
        if (request.ChunkX == (uint)session.CowX / PlantWorld.PageEdge &&
            request.ChunkY == (uint)session.CowY / PlantWorld.PageEdge)
            await session.SendAsync(new AnimalPlaceResponse(SpawnActors.CowId, session.CowX, session.CowY), ct);
        if (request.ChunkX == SpawnActors.ChunkX && request.ChunkY == SpawnActors.ChunkY)
        {
            if (!session.WorldVehiclePickedUp)
                await session.SendAsync(new ItemPlaceResponse(SpawnActors.CarId, SpawnActors.CarX, SpawnActors.CarY), ct);
        }
    }
}

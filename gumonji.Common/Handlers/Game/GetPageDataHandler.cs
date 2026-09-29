using gumonji.Common.World;

namespace gumonji.Common.Handlers.Game;

public sealed class GetPageDataHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.GetPageDataRequest;
    public ServerKind Server => ServerKind.Game;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        if (session.State != SessionState.ZoneEntered || payload.Length != 8)
            throw new InvalidDataException("GET_PAGE_DATA before zone entry or truncated");
        var request = GetPageDataRequest.FromBytes(payload.Span);
        await session.SendAsync(TerrainWorld.CreatePage(request.ChunkX, request.ChunkY), ct);
        if (!session.PlantedChunks.Add((request.ChunkX, request.ChunkY)))
            return;
        foreach (var tree in SpawnTrees.InChunk(request.ChunkX, request.ChunkY))
        {
            var state = await session.Accounts.Gameplay.GetPlantAsync(session.Options.HomeZone, tree.Id, ct);
            await session.SendAsync(
                new PlantPlaceResponse(tree.Id, tree.Subtype, tree.Color, (uint)(state?.Fertility ?? (int)tree.Fertility),
                    tree.X, tree.Y, (byte)(state?.Stage ?? 4)),
                ct);
        }
    }
}

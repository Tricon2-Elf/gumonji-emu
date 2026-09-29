using gumonji.Network.Packets.Game;

namespace gumonji.Common.World;

/// <summary>Deterministic 160 by 160 starter world using client tile.tmpl indices.</summary>
public static class TerrainWorld
{
    public const int MapEdge = 160;
    public const int ChunkCount = MapEdge / PageDataResponse.Edge;

    // natural_sand, natural_w1_f1_c0, natural_earth_w1, natural_rock.
    private const ushort Sand = 1;
    private const ushort Grass = 10;
    private const ushort Earth = 60;
    private const ushort Rock = 50;

    // tile.tmpl rows natural_w4..w7_f1_c1 are explicitly labelled water,
    // deep water, deeper water and deepest water in the client data.
    private const ushort Water = 37;
    private const ushort DeepWater = 38;
    private const ushort DeeperWater = 39;
    private const ushort DeepestWater = 40;

    // Shoreline sand rows natural_sand_w1..w4.
    private const ushort WetSand = 54;

    public static (ushort TerrainId, uint Height, uint WaterLevel) Cell(int x, int y)
    {
        if ((uint)x >= MapEdge || (uint)y >= MapEdge)
            throw new ArgumentOutOfRangeException(nameof(x), "world coordinates must be inside the 160 by 160 map");

        var dxSpawn = x - 64;
        var dySpawn = y - 64;
        var spawnDistance = Math.Sqrt(dxSpawn * dxSpawn + dySpawn * dySpawn);

        // Preserve the existing flat sand spawn and tree grove.
        if (spawnDistance <= 18)
            return (Sand, 0, 0);

        // A shallow-to-deep pond east of spawn. Elliptical distance makes a
        // compact shoreline; the ground-height channel carries the basin too.
        var pondDx = (x - 98) / 12.0;
        var pondDy = (y - 64) / 9.0;
        var pondRadius = Math.Sqrt(pondDx * pondDx + pondDy * pondDy);
        if (pondRadius <= 1.0)
        {
            var depth = pondRadius < 0.42 ? DeepestWater
                : pondRadius < 0.67 ? DeeperWater
                : pondRadius < 0.86 ? DeepWater
                : Water;
            // A separate water-level value is what enables the client's water
            // mesh/render pass; water terrain IDs alone draw only blue ground.
            return (depth, (uint)Math.Round(Math.Max(0, 900 * pondRadius * pondRadius)), 1500);
        }
        if (pondRadius <= 1.28)
        {
            var shore = Math.Clamp((int)Math.Round((pondRadius - 1.0) / 0.28 * 3), 0, 3);
            var shoreHeight = (uint)Math.Round((pondRadius - 1.0) / 0.28 * 800);
            return ((ushort)(WetSand + shore), shoreHeight, 0);
        }

        var height = Hills(x, y);
        var roughness = 260 * Math.Sin(x * 0.19) * Math.Cos(y * 0.16)
            + 120 * Math.Sin((x + y) * 0.37);
        height = Math.Max(0, height + roughness);

        // Textured soil on the slopes and exposed rock on higher ground.
        var material = height >= 7200 ? Rock : height >= 1800 ? Earth : Grass;
        return (material, (uint)Math.Round(height), 0);
    }

    public static PageDataResponse CreatePage(uint chunkX, uint chunkY)
    {
        if (chunkX >= ChunkCount || chunkY >= ChunkCount)
            throw new InvalidDataException($"chunk ({chunkX},{chunkY}) is outside the {ChunkCount} by {ChunkCount} map");

        var terrainIds = new ushort[PageDataResponse.Cells];
        var heights = new uint[PageDataResponse.Cells];
        var waterLevels = new uint[PageDataResponse.Cells];
        var startX = checked((int)chunkX * PageDataResponse.Edge);
        var startY = checked((int)chunkY * PageDataResponse.Edge);
        var index = 0;
        for (var y = 0; y < PageDataResponse.Edge; y++)
        for (var x = 0; x < PageDataResponse.Edge; x++)
        {
            var cell = Cell(startX + x, startY + y);
            terrainIds[index] = cell.TerrainId;
            heights[index] = cell.Height;
            waterLevels[index] = cell.WaterLevel;
            index++;
        }

        return new PageDataResponse(chunkX, chunkY, terrainIds, heights, waterLevels);
    }

    private static double Hills(int x, int y)
    {
        // Smooth overlapping hill profiles give distinct, walkable elevation
        // bands. Absolute coordinates keep adjacent chunk edges continuous.
        var height = 700.0
            + Peak(x, y, 34, 36, 7200, 19, 17)
            + Peak(x, y, 130, 39, 5900, 21, 19)
            + Peak(x, y, 37, 125, 8200, 22, 20)
            + Peak(x, y, 126, 124, 6800, 20, 23)
            + Peak(x, y, 82, 132, 4700, 18, 14);

        // Leave a broad level approach around the entrance area.
        var d = Math.Sqrt((x - 64) * (x - 64) + (y - 64) * (y - 64));
        var blend = Math.Clamp((d - 18) / 18.0, 0, 1);
        return height * blend;
    }

    private static double Peak(int x, int y, int centerX, int centerY, double height, double radiusX, double radiusY)
    {
        var dx = (x - centerX) / radiusX;
        var dy = (y - centerY) / radiusY;
        return height * Math.Exp(-0.5 * (dx * dx + dy * dy));
    }
}

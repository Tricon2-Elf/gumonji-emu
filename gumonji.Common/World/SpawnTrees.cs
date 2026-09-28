namespace gumonji.Common.World;

public readonly record struct SpawnTree(uint Id, byte Subtype, byte Color, uint Fertility, ushort X, ushort Y);

/// <summary>
/// The scatter produced by Python's random.Random(1) around cell 64,64.
/// Keeping the cells fixed makes the C# world match the Python server.
/// </summary>
public static class SpawnTrees
{
    public const int PageEdge = 32;

    public static readonly SpawnTree[] All =
    [
        new(1000, 1, 8, 36000, 70, 69),
        new(1001, 2, 8, 100000, 72, 69),
        new(1002, 3, 8, 24000, 75, 75),
        new(1003, 4, 8, 40000, 75, 74),
        new(1004, 5, 8, 8000, 71, 69),
        new(1005, 1, 8, 36000, 75, 68),
        new(1006, 2, 8, 100000, 74, 74),
        new(1007, 3, 8, 24000, 68, 75),
        new(1008, 4, 8, 40000, 56, 71),
        new(1009, 5, 8, 8000, 59, 73),
        new(1010, 1, 8, 36000, 60, 68),
        new(1011, 2, 8, 100000, 60, 76),
        new(1012, 3, 8, 24000, 68, 54),
        new(1013, 4, 8, 40000, 71, 54),
        new(1014, 5, 8, 8000, 68, 52),
        new(1015, 1, 8, 36000, 71, 53),
        new(1016, 2, 8, 100000, 53, 52),
        new(1017, 3, 8, 24000, 57, 55),
        new(1018, 4, 8, 40000, 57, 57),
        new(1019, 5, 8, 8000, 53, 56),
    ];

    public static IEnumerable<SpawnTree> InChunk(uint chunkX, uint chunkY) =>
        All.Where(tree => tree.X / PageEdge == chunkX && tree.Y / PageEdge == chunkY);
}

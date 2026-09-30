namespace gumonji.Common.World;

/// <summary>Green tree and matching seed templates from plant.tmpl and item.tmpl.</summary>
public static class TreeSeeds
{
    public const int ItemType = 92;
    public const int Green = 8;

    public static bool IsSupported(int subtype, int color) =>
        color == Green && subtype is >= 1 and <= 5;

    public static string Name(int subtype) => subtype switch
    {
        1 => "normal tree",
        2 => "bean tree",
        3 => "palm tree",
        4 => "triangle tree",
        5 => "bamboo tree",
        _ => throw new ArgumentOutOfRangeException(nameof(subtype)),
    };
}

namespace gumonji.Network.Packets.Zone;

public static class PlayerAppearance
{
    public static readonly int[] StyleEyes = [0, 13, 12, 7, 4, 3, 2, 1];

    public static (byte Type, byte Subtype, byte ColorType, byte Eye, byte Tint) Resolve(
        int body, int model, int style, int color) => (1,
        (byte)(body is 0 or 1 ? body : 0),
        (byte)(model is >= 0 and <= 16 ? model : 0),
        (byte)(style >= 0 && style < StyleEyes.Length ? StyleEyes[style] : 0),
        (byte)(color is >= 0 and < 100 ? color : 0));
}

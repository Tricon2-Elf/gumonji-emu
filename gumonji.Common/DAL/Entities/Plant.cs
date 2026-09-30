namespace gumonji.Common.DAL.Entities;

/// <summary>A plant in the zone, whether part of the original grove or planted by a player.</summary>
public sealed class Plant
{
    public int ZoneId { get; set; }
    public int Id { get; set; }
    public int? CharacterId { get; set; }
    public int Type { get; set; }
    public int Subtype { get; set; }
    public int Color { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Fertility { get; set; }
    public int Stage { get; set; }
}

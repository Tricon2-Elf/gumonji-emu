namespace gumonji.Common.DAL.Entities;

public sealed class PlantState
{
    public int ZoneId { get; set; }
    public int PlantId { get; set; }
    public int Fertility { get; set; }
    public int Stage { get; set; } = 4;
}

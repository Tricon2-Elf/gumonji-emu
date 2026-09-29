namespace gumonji.Common.DAL.Entities;

public sealed class InventoryItem
{
    public int Id { get; set; }
    public int CharacterId { get; set; }
    public int Slot { get; set; }
    public int ItemType { get; set; }
    public int Subtype { get; set; }
    public int Color { get; set; }
    public int Fertility { get; set; }
}

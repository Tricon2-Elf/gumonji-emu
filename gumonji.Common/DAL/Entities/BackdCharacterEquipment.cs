namespace gumonji.Common.DAL.Entities;

public sealed class BackdCharacterEquipment
{
    public long UserId { get; set; }
    public int Slot { get; set; }
    public byte Value { get; set; }
}

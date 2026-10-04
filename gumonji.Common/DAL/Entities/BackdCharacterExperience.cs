namespace gumonji.Common.DAL.Entities;

public sealed class BackdCharacterExperience
{
    public long UserId { get; set; }
    public int Index { get; set; }
    public byte Category { get; set; }
    public byte Type { get; set; }
    public byte Subtype { get; set; }
    public byte Color { get; set; }
}

namespace gumonji.Common.DAL.Entities;

public sealed class BackdCharacterItemParameter
{
    public long UserId { get; set; }
    public int Slot { get; set; }
    public bool Secret { get; set; }
    public int Index { get; set; }
    public int Value { get; set; }
}

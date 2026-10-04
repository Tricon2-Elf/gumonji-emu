namespace gumonji.Common.DAL.Entities;

public sealed class BackdCharacterItemComment
{
    public long UserId { get; set; }
    public int Slot { get; set; }
    public int Index { get; set; }
    public byte[] Text { get; set; } = [];
}

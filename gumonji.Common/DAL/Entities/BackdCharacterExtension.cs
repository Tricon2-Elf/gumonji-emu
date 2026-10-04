namespace gumonji.Common.DAL.Entities;

/// <summary>Unrecognized directives/comments retained individually for compatibility.</summary>
public sealed class BackdCharacterExtension
{
    public long UserId { get; set; }
    public int Index { get; set; }
    public byte[] Line { get; set; } = [];
}

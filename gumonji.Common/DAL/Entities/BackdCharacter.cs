namespace gumonji.Common.DAL.Entities;

/// <summary>The original zone server's packed character record, kept byte-for-byte.</summary>
public sealed class BackdCharacter
{
    public long UserId { get; set; }
    public byte[] Payload { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

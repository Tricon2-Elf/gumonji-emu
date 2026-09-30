namespace gumonji.Common.DAL.Entities;

/// <summary>The original backend's 24-element user history record.</summary>
public sealed class BackdHistory
{
    public long UserId { get; set; }
    public byte[] Payload { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

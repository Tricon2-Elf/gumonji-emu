namespace gumonji.Common.DAL.Entities;

public sealed class Character
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public byte[] Name { get; set; } = [];
    public int Body { get; set; }
    public int Model { get; set; }
    public int Style { get; set; }
    public int Color { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = default!;
}

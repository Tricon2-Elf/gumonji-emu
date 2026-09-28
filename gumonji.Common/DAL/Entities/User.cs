namespace gumonji.Common.DAL.Entities;

public sealed class User
{
    public int Id { get; set; }
    public byte[] Username { get; set; } = [];
    public byte[] PasswordHash { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Character> Characters { get; set; } = new List<Character>();
    public ICollection<LoginToken> LoginTokens { get; set; } = new List<LoginToken>();
}

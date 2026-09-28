namespace gumonji.Common.DAL.Entities;

public sealed class LoginToken
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }

    public User User { get; set; } = default!;
}

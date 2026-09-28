using System.Security.Cryptography;
using System.Text;
using gumonji.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace gumonji.Common.DAL.Repositories;

public interface ILoginTokenRepository
{
    Task<byte[]> IssueAsync(uint userId, string? tokenOverride, CancellationToken ct = default);
    Task<bool> ConsumeAsync(uint userId, byte[] token, CancellationToken ct = default);
}

public sealed class LoginTokenRepository(IDbContextFactory<MainContext> factory) : ILoginTokenRepository
{
    public async Task<byte[]> IssueAsync(uint userId, string? tokenOverride, CancellationToken ct = default)
    {
        var token = string.IsNullOrEmpty(tokenOverride)
            ? Convert.ToHexString(RandomNumberGenerator.GetBytes(10)).ToLowerInvariant()
            : tokenOverride;
        var raw = Encoding.ASCII.GetBytes(token);
        if (raw.Length is < 1 or > 127 || raw.Contains((byte)0))
            throw new InvalidDataException("OTP must contain 1..127 non-NUL ASCII bytes");

        await using var db = await factory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        await db.LoginTokens.Where(x => x.ExpiresAt <= now).ExecuteDeleteAsync(ct);
        var existing = await db.LoginTokens.SingleOrDefaultAsync(x => x.Token == token, ct);
        if (existing is null)
        {
            db.LoginTokens.Add(new LoginToken
            {
                Token = token,
                UserId = checked((int)userId),
                ExpiresAt = now.AddMinutes(2),
            });
        }
        else
        {
            existing.UserId = checked((int)userId);
            existing.ExpiresAt = now.AddMinutes(2);
        }
        await db.SaveChangesAsync(ct);
        return raw;
    }

    public async Task<bool> ConsumeAsync(uint userId, byte[] token, CancellationToken ct = default)
    {
        var key = Encoding.ASCII.GetString(token);
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await db.LoginTokens.SingleOrDefaultAsync(x =>
            x.Token == key && x.UserId == (int)userId && x.ExpiresAt > DateTime.UtcNow, ct);
        if (row is null)
            return false;
        db.LoginTokens.Remove(row);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

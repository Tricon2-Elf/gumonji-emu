using gumonji.Common.DAL.Entities;
using gumonji.Common.Security;
using Microsoft.EntityFrameworkCore;

namespace gumonji.Common.DAL.Repositories;

public interface IAccountRepository
{
    Task<uint> GetOrCreateAsync(byte[] username, byte[] password, CancellationToken ct = default);
    Task<byte[]> GetTutorialFlagsAsync(uint userId, CancellationToken ct = default);
    Task<bool> CompleteTutorialAsync(uint userId, uint tutorialId, CancellationToken ct = default);
}

public sealed class AccountRepository(IDbContextFactory<MainContext> factory) : IAccountRepository
{
    public async Task<byte[]> GetTutorialFlagsAsync(uint userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var ids = await db.TutorialCompletions.AsNoTracking()
            .Where(x => x.UserId == (int)userId)
            .Select(x => x.TutorialId).ToListAsync(ct);
        var flags = new byte[100];
        foreach (var id in ids)
            if ((uint)id < (uint)flags.Length)
                flags[id] = 1;
        return flags;
    }

    public async Task<bool> CompleteTutorialAsync(uint userId, uint tutorialId, CancellationToken ct = default)
    {
        if (tutorialId >= 100)
            return false;
        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await db.Users.AnyAsync(x => x.Id == (int)userId, ct))
            return false;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT OR IGNORE INTO \"TutorialCompletions\" (\"UserId\", \"TutorialId\") VALUES ({(int)userId}, {(int)tutorialId})", ct);
        return true;
    }

    public async Task<uint> GetOrCreateAsync(
        byte[] username,
        byte[] password,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == username, ct);
        if (user is not null)
        {
            if (!PasswordHasher.Verify(password, user.PasswordHash))
                throw new InvalidDataException("invalid account password");
            return checked((uint)user.Id);
        }

        user = new User
        {
            Username = username.ToArray(),
            PasswordHash = PasswordHasher.Hash(password),
            CreatedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A simultaneous first login may have won the unique username race.
            return await GetOrCreateAsync(username, password, ct);
        }
        return checked((uint)user.Id);
    }
}

using gumonji.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace gumonji.Common.DAL.Repositories;

public interface ICharacterRepository
{
    Task<Character?> GetByUserIdAsync(uint userId, CancellationToken ct = default);
    Task SaveAsync(uint userId, Character character, CancellationToken ct = default);
}

public sealed class CharacterRepository(IDbContextFactory<MainContext> factory) : ICharacterRepository
{
    public async Task<Character?> GetByUserIdAsync(uint userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Characters.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
    }

    public async Task SaveAsync(uint userId, Character character, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.Characters.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (existing is null)
        {
            character.UserId = checked((int)userId);
            character.CreatedAt = DateTime.UtcNow;
            character.UpdatedAt = character.CreatedAt;
            db.Characters.Add(character);
        }
        else
        {
            existing.Name = character.Name.ToArray();
            existing.Body = character.Body;
            existing.Model = character.Model;
            existing.Style = character.Style;
            existing.Color = character.Color;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }
}

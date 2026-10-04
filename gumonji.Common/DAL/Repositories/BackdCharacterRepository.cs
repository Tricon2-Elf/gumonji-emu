using gumonji.Common.Backd;
using gumonji.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace gumonji.Common.DAL.Repositories;

public interface IBackdCharacterRepository
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<BackdCharacter?> GetByUserIdAsync(uint userId, CancellationToken ct = default);
    Task SaveAsync(BackdCharacter character, CancellationToken ct = default);
}

public sealed class BackdCharacterRepository(IDbContextFactory<MainContext> factory) : IBackdCharacterRepository
{
    private readonly SemaphoreSlim _initialization = new(1, 1);
    private bool _initialized;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _initialization.WaitAsync(ct);
        try
        {
            if (_initialized) return;
            await using var db = await factory.CreateDbContextAsync(ct);
            await BackdCharacterDataMigration.ConvertAsync(db, ct);
            _initialized = true;
        }
        finally { _initialization.Release(); }
    }

    public async Task<BackdCharacter?> GetByUserIdAsync(uint userId, CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        // Split queries must observe one snapshot while a full character graph is replaced.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var result = await db.BackdCharacters.AsNoTracking()
            .Include(x => x.Parameters).Include(x => x.Equipment).Include(x => x.Experiences)
            .Include(x => x.Extensions).Include(x => x.Items).ThenInclude(x => x.Parameters)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.UserId == userId, ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    public async Task SaveAsync(BackdCharacter character, CancellationToken ct = default)
    {
        // Validate serialization before touching committed state.
        _ = BackdCharacterCodec.Encode(character);
        await InitializeAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.BackdCharacters.Where(x => x.UserId == character.UserId).ExecuteDeleteAsync(ct);
        character.UpdatedAt = DateTime.UtcNow;
        db.BackdCharacters.Add(character);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}

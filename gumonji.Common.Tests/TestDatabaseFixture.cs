using gumonji.Common.DAL;
using gumonji.Common.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace gumonji.Common.Tests;

/// <summary>Owns an isolated SQLite database for protocol tests.</summary>
internal sealed class TestDatabaseFixture : IDbContextFactory<MainContext>, IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"gumonji-protocol-test-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<MainContext> _options;
    public IAccountRepository Accounts { get; }
    public ICharacterRepository Characters { get; }
    public ILoginTokenRepository LoginTokens { get; }
    public IGameplayRepository Gameplay { get; }

    public TestDatabaseFixture()
    {
        _options = new DbContextOptionsBuilder<MainContext>()
            .UseSqlite($"Data Source={_path};Pooling=False").Options;
        using var db = CreateDbContext();
        db.Database.Migrate();
        Accounts = new AccountRepository(this);
        Characters = new CharacterRepository(this);
        LoginTokens = new LoginTokenRepository(this);
        Gameplay = new GameplayRepository(this);
    }

    public MainContext CreateDbContext() => new(_options);
    public Task<MainContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());

    public void Dispose()
    {
        File.Delete(_path);
        File.Delete(_path + "-wal");
        File.Delete(_path + "-shm");
    }
}

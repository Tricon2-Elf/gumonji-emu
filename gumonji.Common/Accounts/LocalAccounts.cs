using gumonji.Common.DAL;
using gumonji.Common.DAL.Entities;
using gumonji.Common.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace gumonji.Common.Accounts;

public sealed class LocalAccounts(
    IAccountRepository accounts,
    ICharacterRepository characters,
    ILoginTokenRepository tokens,
    IGameplayRepository gameplay)
{
    private readonly IAccountRepository _accounts = accounts;
    private readonly ICharacterRepository _characters = characters;
    private readonly ILoginTokenRepository _tokens = tokens;
    public IGameplayRepository Gameplay { get; } = gameplay;

    // Convenience constructor retained for protocol tests and small embedders.
    public LocalAccounts() : this(CreateTestServices()) { }

    private LocalAccounts((IAccountRepository, ICharacterRepository, ILoginTokenRepository, IGameplayRepository) services)
        : this(services.Item1, services.Item2, services.Item3, services.Item4) { }

    public Task<uint> LoginAsync(byte[] username, byte[] password, CancellationToken ct = default) =>
        _accounts.GetOrCreateAsync(username, password, ct);

    public Task<byte[]> GetTutorialFlagsAsync(uint userId, CancellationToken ct = default) =>
        _accounts.GetTutorialFlagsAsync(userId, ct);

    public Task<bool> CompleteTutorialAsync(uint userId, uint tutorialId, CancellationToken ct = default) =>
        _accounts.CompleteTutorialAsync(userId, tutorialId, ct);

    public Task<byte[]> IssueAsync(uint userId, string? otpOverride, CancellationToken ct = default) =>
        _tokens.IssueAsync(userId, otpOverride, ct);

    public Task<bool> ConsumeAsync(uint userId, byte[] token, CancellationToken ct = default) =>
        _tokens.ConsumeAsync(userId, token, ct);

    public Task<Character?> GetCharacterAsync(uint userId, CancellationToken ct = default) =>
        _characters.GetByUserIdAsync(userId, ct);

    public Task SaveCharacterAsync(uint userId, Character character, CancellationToken ct = default) =>
        _characters.SaveAsync(userId, character, ct);

    private static (IAccountRepository, ICharacterRepository, ILoginTokenRepository, IGameplayRepository) CreateTestServices()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gumonji-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MainContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        var factory = new TestContextFactory(options);
        using var db = factory.CreateDbContext();
        db.Database.Migrate();
        return (
            new AccountRepository(factory),
            new CharacterRepository(factory),
            new LoginTokenRepository(factory),
            new GameplayRepository(factory));
    }

    private sealed class TestContextFactory(DbContextOptions<MainContext> options)
        : IDbContextFactory<MainContext>
    {
        public MainContext CreateDbContext() => new(options);
        public Task<MainContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(new MainContext(options));
    }
}

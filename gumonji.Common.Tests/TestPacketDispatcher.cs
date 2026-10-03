using gumonji.Common.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace gumonji.Common.Tests;

internal static class TestPacketDispatcher
{
    public static PacketDispatcher Create(IDbContextFactory<MainContext>? factory = null,
        EmuOptions? options = null, BackdState? state = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(options ?? new EmuOptions());
        services.AddSingleton<IDbContextFactory<MainContext>>(factory ?? new EmptyContextFactory());
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        if (state is not null) services.AddSingleton(state);
        services.AddPacketHandlers();
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<PacketDispatcher>();
    }

    // Handler registration needs a factory, but wire-only tests never access backend data.
    private sealed class EmptyContextFactory : IDbContextFactory<MainContext>
    {
        public MainContext CreateDbContext() => new(new DbContextOptionsBuilder<MainContext>()
            .UseSqlite("Data Source=:memory:").Options);
        public Task<MainContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
    }
}

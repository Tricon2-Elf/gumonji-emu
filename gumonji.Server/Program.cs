using System.Net;
using gumonji.Common;
using gumonji.Common.Accounts;
using gumonji.Common.DAL;
using gumonji.Common.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using gumonji.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace gumonji.Server;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var options = ParseArgs(args);
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = [],
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(console =>
        {
            console.SingleLine = true;
            console.TimestampFormat = "HH:mm:ss ";
        });
        builder.Logging.SetMinimumLevel(options.Verbose ? LogLevel.Debug : LogLevel.Information);
        builder.Services.AddSingleton(options);
        builder.Services.AddDbContextFactory<MainContext>(db =>
            db.UseSqlite($"Data Source={Path.GetFullPath(options.DatabasePath)}"));
        builder.Services.AddSingleton<IAccountRepository, AccountRepository>();
        builder.Services.AddSingleton<ICharacterRepository, CharacterRepository>();
        builder.Services.AddSingleton<ILoginTokenRepository, LoginTokenRepository>();
        builder.Services.AddSingleton<IGameplayRepository, GameplayRepository>();
        builder.Services.AddSingleton<LocalAccounts>();
        builder.Services.AddSingleton(sp => PacketDispatcher.CreateDefault(sp.GetRequiredService<ILogger<PacketDispatcher>>()));
        builder.Services.AddHostedService<GumonjiHost>();
        await builder.Build().RunAsync();
    }

    private static EmuOptions ParseArgs(string[] args)
    {
        var options = new EmuOptions();
        return options;
    }
}

public sealed class GumonjiHost(
    EmuOptions options,
    LocalAccounts accounts,
    PacketDispatcher dispatcher,
    IDbContextFactory<MainContext> dbFactory,
    ILogger<GumonjiHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using (var db = await dbFactory.CreateDbContextAsync(stoppingToken))
            await db.Database.MigrateAsync(stoppingToken);

        var bind = IPAddress.Parse(options.BindAddress);
        var femsg = new VceListener(logger, "frontend", ServerKind.Femsg, new IPEndPoint(bind, options.FemsgPort), Attach, OnPacket);
        var game = new VceListener(logger, "game", ServerKind.Game, new IPEndPoint(bind, options.GamePort), Attach, OnPacket);
        await Task.WhenAll(femsg.RunAsync(stoppingToken), game.RunAsync(stoppingToken));
    }

    private object Attach(ClientConnection connection) =>
        new GumonjiSession(connection.Kind, accounts, options, (type, body, ct) => connection.SendAsync(type, body, ct));

    private async Task OnPacket(ClientConnection connection, PacketType type, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        var session = (GumonjiSession)connection.Session!;
        var previous = session.State;
        session.SilentNoReply = false;
        var sent = session.SentCount;
        var known = await dispatcher.DispatchAsync(session.Kind, type, body, session, ct);
        if (session.State != previous)
            logger.LogInformation("{Label} STATE {Previous} -> {State}", connection.Label, previous, session.State);
        if (!known || (session.SentCount == sent && !session.SilentNoReply))
            logger.LogWarning(
                "{Label} NO_REPLY_IMPLEMENTED state={State} opcode=0x{Opcode:X}",
                connection.Label,
                session.State,
                (uint)type);
    }
}

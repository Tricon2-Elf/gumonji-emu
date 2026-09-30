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
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
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
        builder.Services.AddBackdProtocol();
        builder.Services.AddHostedService<BackdHost>();
        var host = builder.Build();
        await using (var db = await host.Services.GetRequiredService<IDbContextFactory<MainContext>>()
            .CreateDbContextAsync())
            await db.Database.MigrateAsync();
        await host.RunAsync();
    }

    private static EmuOptions ParseArgs(string[] args)
    {
        if (args.Any(arg => arg is not "--backd-only" and not "--no-zone" and not "--no-game" and not "--help"))
            throw new ArgumentException("supported options: --backd-only, --no-zone, --help (--no-game is an alias)");
        if (args.Contains("--help"))
        {
            Console.WriteLine("Usage: dotnet run --project gumonji.Server -- [--backd-only | --no-zone]");
            Environment.Exit(0);
        }
        var options = new EmuOptions
        {
            BackdPassword = Environment.GetEnvironmentVariable("GUMONJI_BACKD_PASSWORD"),
            BackdOnly = args.Contains("--backd-only"),
            EnableZone = !args.Contains("--no-zone") && !args.Contains("--no-game"),
        };
        return options;
    }
}

public sealed class GumonjiHost(
    EmuOptions options,
    LocalAccounts accounts,
    PacketDispatcher dispatcher,
    ILogger<GumonjiHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.BackdOnly)
            return;
        logger.LogInformation("SQLite database: {Path}", Path.GetFullPath(options.DatabasePath));

        var bind = IPAddress.Parse(options.BindAddress);
        var femsg = new VceListener(logger, "frontend", ServerKind.Femsg, new IPEndPoint(bind, options.FemsgPort), Attach, OnPacket);
        if (!options.EnableZone)
        {
            logger.LogInformation("zone listener disabled; port {Port} is available for zonesv", options.ZonePort);
            await femsg.RunAsync(stoppingToken);
            return;
        }

        var zone = new VceListener(logger, "zone", ServerKind.Zone, new IPEndPoint(bind, options.ZonePort), Attach, OnPacket);
        await Task.WhenAll(femsg.RunAsync(stoppingToken), zone.RunAsync(stoppingToken));
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
        {
            logger.LogWarning(
                "{Label} NO_REPLY_IMPLEMENTED state={State} opcode=0x{Opcode:X} payload={Payload}",
                connection.Label,
                session.State,
                (uint)type,
                (uint)type is 0x200 or 0x200A ? Convert.ToHexString(body.Span) : "(not captured)");
        }
    }
}

using System.Net;
using gumonji.Common;
using gumonji.Network;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace gumonji.Server;

public sealed class BackdHost(EmuOptions options, PacketDispatcher dispatcher, BackdState state,
    ILogger<BackdHost> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new VceListener(
            logger,
            "backd",
            ServerKind.Backd, // 16-bit IDs and no game compression envelope.
            new IPEndPoint(IPAddress.Parse(options.BackdBindAddress), options.BackdPort),
            connection => new BackdSession((type, body, ct) => connection.SendAsync(type, body, ct)),
            async (connection, opcode, body, ct) =>
            {
                var session = (BackdSession)connection.Session!;
                await dispatcher.DispatchAsync(opcode, body, session, ct);
            },
            connection => state.Disconnect((BackdSession)connection.Session!));
        return listener.RunAsync(stoppingToken);
    }
}

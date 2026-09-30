using System.Net;
using gumonji.Common;
using gumonji.Network;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace gumonji.Server;

public sealed class BackdHost(EmuOptions options, BackdProtocol protocol, ILogger<BackdHost> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new VceListener(
            logger,
            "backd",
            ServerKind.Backd, // 16-bit IDs and no game compression envelope.
            new IPEndPoint(IPAddress.Parse(options.BackdBindAddress), options.BackdPort),
            _ => new BackdSession(),
            async (connection, opcode, body, ct) =>
            {
                var session = (BackdSession)connection.Session!;
                await protocol.HandleAsync(session, opcode, body,
                    (replyType, replyBody, token) => connection.SendAsync(replyType, replyBody, token), ct);
            },
            connection => protocol.Disconnect((BackdSession)connection.Session!));
        return listener.RunAsync(stoppingToken);
    }
}

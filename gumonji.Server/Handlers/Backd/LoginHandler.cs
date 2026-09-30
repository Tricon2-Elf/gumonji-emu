using gumonji.Common;
using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Server.Handlers.Backd;

public sealed class LoginHandler(EmuOptions options, ILogger<BackdProtocol> logger) : BackdHandler<LoginRequest>
{
    public override PacketType RequestType => PacketType.BackdLoginRequest;

    public override Task<IOutgoingPacket?> HandleAsync(LoginRequest request,
        BackdSession session, CancellationToken ct)
    {
        var name = System.Text.Encoding.ASCII.GetString(request.ZoneName);
        var accepted = request.ZoneName.Length is > 0 and <= 16 &&
            (options.BackdPassword is null ||
             System.Text.Encoding.UTF8.GetBytes(options.BackdPassword).AsSpan().SequenceEqual(request.Password));
        if (accepted) session.ZoneName = name;
        logger.LogInformation("backd login zone={Zone} accepted={Accepted}", name, accepted);
        return Task.FromResult<IOutgoingPacket?>(new LoginReply(
            accepted ? 0u : unchecked((uint)-7), 0u, [], 1u));
    }
}

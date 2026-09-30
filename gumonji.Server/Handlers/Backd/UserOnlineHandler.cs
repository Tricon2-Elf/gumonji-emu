using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Server.Handlers.Backd;

public sealed class UserOnlineHandler(BackdState state, ILogger<BackdProtocol> logger) : BackdHandler<UserOnlineRequest>
{
    public override PacketType RequestType => PacketType.BackdUserOnlineRequest;

    public override Task<IOutgoingPacket?> HandleAsync(UserOnlineRequest request,
        BackdSession session, CancellationToken ct)
    {
        var alreadyOnline = state.UserOnline(request.UserId, session.ZoneName!);
        logger.LogInformation("backd user online zone={Zone} uid={UserId} alreadyOnline={AlreadyOnline}",
            session.ZoneName, request.UserId, alreadyOnline);
        return Task.FromResult<IOutgoingPacket?>(null);
    }
}

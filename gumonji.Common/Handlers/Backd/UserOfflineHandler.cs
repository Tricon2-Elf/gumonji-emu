using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class UserOfflineHandler(BackdState state, ILogger<UserOfflineHandler> logger) : BackdHandler<UserOfflineRequest>
{
    public override PacketType RequestType => PacketType.BackdUserOfflineRequest;

    public override Task HandleAsync(UserOfflineRequest request,
        BackdSession session, CancellationToken ct)
    {
        var removed = state.UserOffline(request.UserId, session.ZoneName!);
        logger.LogInformation("backd user offline zone={Zone} uid={UserId} wasOnline={WasOnline}",
            session.ZoneName, request.UserId, removed);
        return Task.CompletedTask;
    }
}

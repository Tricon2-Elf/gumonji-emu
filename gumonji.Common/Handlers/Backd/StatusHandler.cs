using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class StatusHandler : BackdHandler<StatusRequest>
{
    public override PacketType RequestType => PacketType.BackdStatusRequest;

    public override Task HandleAsync(StatusRequest request,
        BackdSession session, CancellationToken ct)
    {
        return session.SendAsync(new StatusReply(10001u, 184022225u, 0u), ct);
    }
}

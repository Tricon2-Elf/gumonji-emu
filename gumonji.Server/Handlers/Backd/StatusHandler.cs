using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Server.Handlers.Backd;

public sealed class StatusHandler : BackdHandler<StatusRequest>
{
    public override PacketType RequestType => PacketType.BackdStatusRequest;

    public override Task<IOutgoingPacket?> HandleAsync(StatusRequest request,
        BackdSession session, CancellationToken ct)
    {
        return Task.FromResult<IOutgoingPacket?>(new StatusReply(10001u, 184022225u, 0u));
    }
}

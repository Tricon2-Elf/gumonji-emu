using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class PassageLinksHandler : BackdHandler<PassageLinksRequest>
{
    public override PacketType RequestType => PacketType.BackdPassageLinksRequest;

    public override Task<IOutgoingPacket?> HandleAsync(PassageLinksRequest request,
        BackdSession session, CancellationToken ct)
    {
        return Task.FromResult<IOutgoingPacket?>(new PassageLinksReply(request.ZoneId));
    }
}

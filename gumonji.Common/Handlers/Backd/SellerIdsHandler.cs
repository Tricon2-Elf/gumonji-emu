using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class SellerIdsHandler(ILogger<BackdProtocol> logger) : BackdHandler<SellerIdsRequest>
{
    public override PacketType RequestType => PacketType.BackdSellerIdsRequest;

    public override Task<IOutgoingPacket?> HandleAsync(SellerIdsRequest request,
        BackdSession session, CancellationToken ct)
    {
        logger.LogInformation("backd seller IDs zone={Zone} count={Count}",
            session.ZoneName, request.Ids.Length);
        return Task.FromResult<IOutgoingPacket?>(null);
    }
}

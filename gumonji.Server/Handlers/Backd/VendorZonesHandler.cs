using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Server.Handlers.Backd;

public sealed class VendorZonesHandler : BackdHandler<VendorZonesRequest>
{
    public override PacketType RequestType => PacketType.BackdVendorZonesRequest;

    public override Task<IOutgoingPacket?> HandleAsync(VendorZonesRequest request,
        BackdSession session, CancellationToken ct)
    {
        return Task.FromResult<IOutgoingPacket?>(new VendorZonesReply(request.MessageId));
    }
}

using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class VendorZonesHandler : BackdHandler<VendorZonesRequest>
{
    public override PacketType RequestType => PacketType.BackdVendorZonesRequest;

    public override Task HandleAsync(VendorZonesRequest request,
        BackdSession session, CancellationToken ct)
    {
        return session.SendAsync(new VendorZonesReply(request.MessageId), ct);
    }
}

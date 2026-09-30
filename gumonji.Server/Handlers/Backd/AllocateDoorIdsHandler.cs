using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Server.Handlers.Backd;

public sealed class AllocateDoorIdsHandler(BackdState state) : BackdHandler<AllocateDoorIdsRequest>
{
    public override PacketType RequestType => PacketType.BackdAllocateDoorIdsRequest;

    public override async Task<IOutgoingPacket?> HandleAsync(AllocateDoorIdsRequest request,
        BackdSession session, CancellationToken ct)
    {
        var ids = await state.AllocateDoorIdsAsync(request.Count, ct);
        return new AllocateDoorIdsReply(ids);
    }
}

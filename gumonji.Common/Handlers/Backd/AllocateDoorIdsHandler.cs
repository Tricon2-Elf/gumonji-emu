using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class AllocateDoorIdsHandler(BackdState state) : BackdHandler<AllocateDoorIdsRequest>
{
    public override PacketType RequestType => PacketType.BackdAllocateDoorIdsRequest;

    public override async Task HandleAsync(AllocateDoorIdsRequest request,
        BackdSession session, CancellationToken ct)
    {
        var ids = await state.AllocateDoorIdsAsync(request.Count, ct);
        await session.SendAsync(new AllocateDoorIdsReply(ids), ct);
    }
}

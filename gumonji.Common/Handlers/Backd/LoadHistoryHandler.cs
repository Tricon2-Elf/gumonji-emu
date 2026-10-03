using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class LoadHistoryHandler(BackdState state) : BackdHandler<LoadHistoryRequest>
{
    public override PacketType RequestType => PacketType.BackdLoadHistoryRequest;

    public override async Task HandleAsync(LoadHistoryRequest request,
        BackdSession session, CancellationToken ct)
    {
        var values = await state.LoadHistoryAsync(request.UserId, ct);
        await session.SendAsync(new LoadHistoryReply(request.MessageId, request.UserId, values), ct);
    }
}

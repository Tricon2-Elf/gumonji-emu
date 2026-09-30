using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Server.Handlers.Backd;

public sealed class LoadHistoryHandler(BackdState state) : BackdHandler<LoadHistoryRequest>
{
    public override PacketType RequestType => PacketType.BackdLoadHistoryRequest;

    public override async Task<IOutgoingPacket?> HandleAsync(LoadHistoryRequest request,
        BackdSession session, CancellationToken ct)
    {
        var values = await state.LoadHistoryAsync(request.UserId, ct);
        return new LoadHistoryReply(request.MessageId, request.UserId, values);
    }
}

using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class GetLockHandler(BackdState state) : BackdHandler<GetLockRequest>
{
    public override PacketType RequestType => PacketType.BackdGetLockRequest;

    public override Task HandleAsync(GetLockRequest request,
        BackdSession session, CancellationToken ct)
    {
        var acquired = state.GetLock(request.UserId, session.Id);
        return session.SendAsync(new GetLockReply(request.MessageId,
            acquired ? 0u : unchecked((uint)-36), request.UserId), ct);
    }
}

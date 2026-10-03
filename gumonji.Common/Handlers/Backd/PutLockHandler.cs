using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class PutLockHandler(BackdState state) : BackdHandler<PutLockRequest>
{
    public override PacketType RequestType => PacketType.BackdPutLockRequest;

    public override Task<IOutgoingPacket?> HandleAsync(PutLockRequest request,
        BackdSession session, CancellationToken ct)
    {
        var released = state.PutLock(request.UserId, session.Id);
        return Task.FromResult<IOutgoingPacket?>(new PutLockReply(request.MessageId,
            released ? 0u : unchecked((uint)-36), request.UserId));
    }
}

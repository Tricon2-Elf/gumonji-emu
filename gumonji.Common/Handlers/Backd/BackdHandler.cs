using gumonji.Network;
using gumonji.Network.Packets.Backd;

namespace gumonji.Common.Handlers.Backd;

public interface IBackdHandler
{
    PacketType RequestType { get; }
    Task<IOutgoingPacket?> HandleAsync(ReadOnlyMemory<byte> payload, BackdSession session,
        CancellationToken ct);
}

public abstract class BackdHandler<TRequest> : IBackdHandler
    where TRequest : IIncomingPacket<TRequest>
{
    public abstract PacketType RequestType { get; }
    public abstract Task<IOutgoingPacket?> HandleAsync(TRequest request, BackdSession session,
        CancellationToken ct);

    public Task<IOutgoingPacket?> HandleAsync(ReadOnlyMemory<byte> payload, BackdSession session,
        CancellationToken ct) => HandleAsync(TRequest.FromBytes(payload.Span), session, ct);
}

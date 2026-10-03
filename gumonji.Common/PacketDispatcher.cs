using Microsoft.Extensions.Logging;

namespace gumonji.Common;

public interface IPacketHandler
{
    PacketType RequestType { get; }
    ServerKind Server { get; }
    Task HandleAsync(ReadOnlyMemory<byte> payload, IPacketSession session, CancellationToken ct = default);
}

public abstract class SessionPacketHandler<TSession> : IPacketHandler
    where TSession : class, IPacketSession
{
    public abstract PacketType RequestType { get; }
    public abstract ServerKind Server { get; }
    public abstract Task HandleAsync(ReadOnlyMemory<byte> payload, TSession session, CancellationToken ct);

    Task IPacketHandler.HandleAsync(ReadOnlyMemory<byte> payload, IPacketSession session, CancellationToken ct)
    {
        if (session is not TSession typed)
            throw new InvalidDataException($"packet 0x{(uint)RequestType:X} requires {typeof(TSession).Name}");
        return HandleAsync(payload, typed, ct);
    }
}

public abstract class PacketHandlerBase<TRequest, TSession> : SessionPacketHandler<TSession>
    where TRequest : IIncomingPacket<TRequest>
    where TSession : class, IPacketSession
{
    public abstract Task HandleAsync(TRequest request, TSession session, CancellationToken ct);

    public sealed override Task HandleAsync(ReadOnlyMemory<byte> payload, TSession session, CancellationToken ct)
    {
        var request = TRequest.FromBytes(payload.Span);
        return HandleAsync(request, session, ct);
    }
}

public abstract class PacketHandlerBase<TRequest> : PacketHandlerBase<TRequest, GumonjiSession>
    where TRequest : IIncomingPacket<TRequest>;

public sealed class PacketDispatcher
{
    private readonly Dictionary<(ServerKind Server, PacketType Type), IPacketHandler> _handlers;

    public PacketDispatcher(IEnumerable<IPacketHandler> handlers, ILogger<PacketDispatcher> logger)
    {
        _handlers = handlers.ToDictionary(handler => (handler.Server, handler.RequestType));
        logger.LogInformation("Registered {Count} packet handlers", _handlers.Count);
    }

    public IReadOnlyCollection<PacketType> GetHandledPacketTypes(ServerKind server) =>
        _handlers.Keys.Where(key => key.Server == server).Select(key => key.Type).ToArray();

    public Task<bool> DispatchAsync(PacketType type, ReadOnlyMemory<byte> payload,
        IPacketSession session, CancellationToken ct = default) => DispatchAsync(session.Kind, type, payload, session, ct);

    public async Task<bool> DispatchAsync(
        ServerKind server,
        PacketType type,
        ReadOnlyMemory<byte> payload,
        IPacketSession session,
        CancellationToken ct = default)
    {
        if (!_handlers.TryGetValue((server, type), out var handler))
            return session.HandleUnknownPacket(type);
        if (session.Kind != server)
            throw new InvalidDataException($"{session.Kind} session cannot receive {server} packets");
        session.ValidateRequest(type);
        await handler.HandleAsync(payload, session, ct);
        return true;
    }
}

using Microsoft.Extensions.Logging;

namespace gumonji.Common;

public interface IPacketHandler
{
    PacketType RequestType { get; }
    ServerKind Server { get; }
    Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct = default);
}

public abstract class PacketHandlerBase<TRequest> : IPacketHandler
    where TRequest : IIncomingPacket<TRequest>
{
    public abstract PacketType RequestType { get; }
    public abstract ServerKind Server { get; }

    public abstract Task HandleAsync(TRequest request, GumonjiSession session, CancellationToken ct);

    public Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        var request = TRequest.FromBytes(payload.Span);
        return HandleAsync(request, session, ct);
    }
}

public sealed class PacketDispatcher
{
    private readonly Dictionary<(ServerKind Server, PacketType Type), IPacketHandler> _handlers;
    private readonly ILogger<PacketDispatcher> _logger;

    public PacketDispatcher(IEnumerable<IPacketHandler> handlers, ILogger<PacketDispatcher> logger)
    {
        _logger = logger;
        _handlers = handlers.ToDictionary(handler => (handler.Server, handler.RequestType));
    }

    public static PacketDispatcher CreateDefault(ILogger<PacketDispatcher> logger)
    {
        var handlers = new List<IPacketHandler>();
        foreach (var type in typeof(PacketDispatcher).Assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(IPacketHandler).IsAssignableFrom(type))
                continue;
            if (type.GetConstructor(Type.EmptyTypes) is null)
                continue;
            handlers.Add((IPacketHandler)Activator.CreateInstance(type)!);
        }

        foreach (var opcode in new[]
        {
            PacketType.ChunkSubscribe0Bcc,
            PacketType.ChunkSubscribe1Fa4,
            PacketType.ChunkSubscribe200C,
            PacketType.ChunkSubscribe203A,
            PacketType.ChunkSubscribe206C,
        })
            handlers.Add(new Handlers.Game.ChunkSubscribeHandler(opcode));

        logger.LogInformation("Registered {Count} packet handlers", handlers.Count);
        return new PacketDispatcher(handlers, logger);
    }

    public async Task<bool> DispatchAsync(
        ServerKind server,
        PacketType type,
        ReadOnlyMemory<byte> payload,
        GumonjiSession session,
        CancellationToken ct = default)
    {
        if (!_handlers.TryGetValue((server, type), out var handler))
            return false;
        await handler.HandleAsync(payload, session, ct);
        return true;
    }
}

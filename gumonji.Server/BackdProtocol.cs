using gumonji.Network;
using gumonji.Server.Handlers.Backd;
using Microsoft.Extensions.Logging;

namespace gumonji.Server;

public sealed class BackdSession
{
    public Guid Id { get; } = Guid.NewGuid();
    public string? ZoneName { get; set; }
    public bool Authenticated => ZoneName is not null;
}

/// <summary>Dispatches original zonesv/backend packets to typed handlers.</summary>
public sealed class BackdProtocol
{
    private readonly Dictionary<PacketType, IBackdHandler> _handlers;
    private readonly BackdState _state;

    public BackdProtocol(BackdState state, IEnumerable<IBackdHandler> handlers,
        ILogger<BackdProtocol> logger)
    {
        _state = state;
        _handlers = handlers.ToDictionary(handler => handler.RequestType);
        logger.LogInformation("Registered {Count} backd packet handlers", _handlers.Count);
    }

    public void Disconnect(BackdSession session) => _state.Disconnect(session);

    public IReadOnlyCollection<PacketType> HandledPacketTypes => _handlers.Keys.ToArray();

    public async Task HandleAsync(BackdSession session, PacketType type, ReadOnlyMemory<byte> body,
        Func<PacketType, byte[], CancellationToken, Task> send, CancellationToken ct = default)
    {
        if (!_handlers.TryGetValue(type, out var handler))
            throw new InvalidDataException($"unsupported backd opcode {(uint)type}");
        if (type != PacketType.BackdLoginRequest && !session.Authenticated)
            throw new InvalidDataException("backd message before frontend_login");

        var response = await handler.HandleAsync(body, session, ct);
        if (response is not null)
            await send(response.Type, response.ToBytes(), ct);
    }
}

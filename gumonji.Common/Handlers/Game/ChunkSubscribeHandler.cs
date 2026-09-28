namespace gumonji.Common.Handlers.Game;

public sealed class ChunkSubscribeHandler(PacketType requestType) : IPacketHandler
{
    public PacketType RequestType { get; } = requestType;
    public ServerKind Server => ServerKind.Game;

    public Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        if (payload.Length != 8)
            throw new InvalidDataException($"invalid CHUNK_SUBSCRIBE size for opcode 0x{(uint)RequestType:X}");
        _ = ChunkSubscribeRequest.FromBytes(payload.Span);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

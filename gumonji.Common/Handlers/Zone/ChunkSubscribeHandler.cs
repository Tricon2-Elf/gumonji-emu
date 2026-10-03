namespace gumonji.Common.Handlers.Zone;

public sealed class ChunkSubscribeHandler(PacketType requestType) : SessionPacketHandler<GumonjiSession>
{
    public override PacketType RequestType { get; } = requestType;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(ReadOnlyMemory<byte> payload, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        if (payload.Length != 8)
            throw new InvalidDataException($"invalid CHUNK_SUBSCRIBE size for opcode 0x{(uint)RequestType:X}");
        _ = ChunkSubscribeRequest.FromBytes(payload.Span);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

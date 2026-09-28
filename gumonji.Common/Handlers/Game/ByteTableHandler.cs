namespace gumonji.Common.Handlers.Game;

public sealed class ByteTableHandler : PacketHandlerBase<ByteTableRequest>
{
    public override PacketType RequestType => PacketType.ByteTableRequest;
    public override ServerKind Server => ServerKind.Game;

    public override Task HandleAsync(ByteTableRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        return session.SendAsync(new ByteTableResponse(), ct);
    }
}

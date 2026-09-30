namespace gumonji.Common.Handlers.Zone;

public sealed class ByteTableHandler : PacketHandlerBase<ByteTableRequest>
{
    public override PacketType RequestType => PacketType.ByteTableRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(ByteTableRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        return session.SendAsync(new ByteTableResponse(), ct);
    }
}

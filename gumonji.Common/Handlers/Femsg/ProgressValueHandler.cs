using gumonji.Network;
using gumonji.Network.Packets.Femsg;

namespace gumonji.Common.Handlers.Femsg;

public sealed class ProgressValueHandler : PacketHandlerBase<ProgressValueRequest>
{
    public override PacketType RequestType => PacketType.ProgressValueRequest;
    public override ServerKind Server => ServerKind.Femsg;

    public override Task HandleAsync(ProgressValueRequest request, GumonjiSession session, CancellationToken ct) =>
        session.SendAsync(new ProgressValueResponse(), ct);
}

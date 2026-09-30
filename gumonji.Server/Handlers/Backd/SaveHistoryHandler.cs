using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Server.Handlers.Backd;

public sealed class SaveHistoryHandler(BackdState state) : BackdHandler<SaveHistoryRequest>
{
    public override PacketType RequestType => PacketType.BackdSaveHistoryRequest;

    public override async Task<IOutgoingPacket?> HandleAsync(SaveHistoryRequest request,
        BackdSession session, CancellationToken ct)
    {
        await state.SaveHistoryAsync(request.Values, ct);
        return new SaveHistoryReply(0u);
    }
}

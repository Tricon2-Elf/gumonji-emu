using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class SaveCharacterHandler(BackdState state) : BackdHandler<SaveCharacterRequest>
{
    public override PacketType RequestType => PacketType.BackdSaveCharacterRequest;

    public override async Task HandleAsync(SaveCharacterRequest request,
        BackdSession session, CancellationToken ct)
    {
        var valid = request.Operation is (uint)'I' or (uint)'C' or (uint)'U';
        if (valid) await state.SaveCharacterAsync(request.UserId, request.Payload, ct);
        await session.SendAsync(new SaveCharacterReply(request.MessageId,
            valid ? 0u : unchecked((uint)-6), request.Operation), ct);
    }
}

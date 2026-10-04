using gumonji.Network.Packets.Backd;
using gumonji.Common.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class LoadCharacterHandler(BackdState state) : BackdHandler<LoadCharacterRequest>
{
    public override PacketType RequestType => PacketType.BackdLoadCharacterRequest;

    public override async Task HandleAsync(LoadCharacterRequest request,
        BackdSession session, CancellationToken ct)
    {
        var character = await state.GetCharacterAsync(request.UserId, ct);
        await session.SendAsync(new LoadCharacterReply(request.MessageId, request.UserId,
            character is null ? unchecked((uint)-13) : 0u,
            character is null ? [] : BackdCharacterCodec.Encode(character), request.Options), ct);
    }
}

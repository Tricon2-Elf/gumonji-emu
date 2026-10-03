using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class CharacterExistsHandler(BackdState state) : BackdHandler<CharacterExistsRequest>
{
    public override PacketType RequestType => PacketType.BackdCharacterExistsRequest;

    public override async Task HandleAsync(CharacterExistsRequest request,
        BackdSession session, CancellationToken ct)
    {
        var character = await state.GetCharacterAsync(request.UserId, ct);
        await session.SendAsync(new CharacterExistsReply(request.MessageId,
            character is null ? unchecked((uint)-13) : 0u), ct);
    }
}

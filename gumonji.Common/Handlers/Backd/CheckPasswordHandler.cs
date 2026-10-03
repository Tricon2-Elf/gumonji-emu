using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;
using CheckPasswordRequest = gumonji.Network.Packets.Backd.CheckPasswordRequest;

namespace gumonji.Common.Handlers.Backd;

public sealed class CheckPasswordHandler(BackdState state) : BackdHandler<CheckPasswordRequest>
{
    public override PacketType RequestType => PacketType.BackdCheckPasswordRequest;

    public override async Task HandleAsync(CheckPasswordRequest request,
        BackdSession session, CancellationToken ct)
    {
        var (result, username) = await state.CheckLoginAsync(request.UserId, request.Token, ct);
        await session.SendAsync(new CheckPasswordReply(request.MessageId, result, request.UserId, 0u,
            result == 0 ? 1u : 0u, username, [], (ushort)(result == 0 ? 1 : 0)), ct);
    }
}

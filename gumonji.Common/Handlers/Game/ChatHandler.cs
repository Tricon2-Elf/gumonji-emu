namespace gumonji.Common.Handlers.Game;

public sealed class ChatHandler : PacketHandlerBase<ChatRequest>
{
    public override PacketType RequestType => PacketType.ChatRequest;
    public override ServerKind Server => ServerKind.Game;

    public override Task HandleAsync(ChatRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        session.LastChat = new ChatState(request.Sender, request.Message);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

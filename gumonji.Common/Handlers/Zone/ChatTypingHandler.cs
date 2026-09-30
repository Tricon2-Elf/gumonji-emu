namespace gumonji.Common.Handlers.Zone;

public sealed class ChatTypingHandler : PacketHandlerBase<ChatTypingRequest>
{
    public override PacketType RequestType => PacketType.ChatTypingRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(ChatTypingRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        session.IsTypingInChat = true;
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

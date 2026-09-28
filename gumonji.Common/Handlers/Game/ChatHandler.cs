namespace gumonji.Common.Handlers.Game;

public sealed class ChatHandler : PacketHandlerBase<ChatRequest>
{
    public override PacketType RequestType => PacketType.ChatRequest;
    public override ServerKind Server => ServerKind.Game;

    public override async Task HandleAsync(ChatRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        session.LastChat = new ChatState(request.Sender, request.Message);

        var character = await session.Accounts.GetCharacterAsync(session.UserId!.Value, ct);
        var sender = character?.Name ?? request.Sender;
        if (sender.Length > 32)
            sender = sender[..32];
        var characterId = session.CharacterId ?? (character is null ? null : checked((uint)character.Id));
        if (characterId is null or 0)
            throw new InvalidDataException("CHAT before character assignment");

        await session.SendAsync(new ChatEventResponse(characterId.Value, sender, request.Message), ct);
    }
}

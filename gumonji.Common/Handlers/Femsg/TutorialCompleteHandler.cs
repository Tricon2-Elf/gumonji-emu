namespace gumonji.Common.Handlers.Femsg;

public sealed class TutorialCompleteHandler : PacketHandlerBase<TutorialCompleteRequest>
{
    public override PacketType RequestType => PacketType.TutorialCompleteRequest;
    public override ServerKind Server => ServerKind.Femsg;

    public override async Task HandleAsync(TutorialCompleteRequest request, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null || request.UserId != session.UserId.Value)
            throw new InvalidDataException("TUTORIAL_COMPLETE for a different or unauthenticated user");
        var saved = await session.Accounts.CompleteTutorialAsync(request.UserId, request.TutorialId, ct);
        await session.SendAsync(new TutorialCompleteResponse(saved), ct);
    }
}

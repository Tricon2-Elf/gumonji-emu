namespace gumonji.Common.Handlers.Femsg;

public sealed class AvatarAnimationHandler : PacketHandlerBase<AvatarAnimationNotice>
{
    public override PacketType RequestType => PacketType.AvatarAnimationNotice;
    public override ServerKind Server => ServerKind.Femsg;

    public override Task HandleAsync(AvatarAnimationNotice request, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null || session.State != SessionState.HandoffIssued)
            throw new InvalidDataException("avatar animation notice before zone handoff");
        session.LastFrontendAnimationId = request.AnimationId;
        // The client sends this on its frontend connection after its local
        // avatar animation changes; no frontend response exists for opcode 420.
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

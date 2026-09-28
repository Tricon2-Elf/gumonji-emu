namespace gumonji.Common.Handlers.Femsg;

public sealed class ProfileRequestHandler : PacketHandlerBase<ProfileRequest>
{
    public override PacketType RequestType => PacketType.ProfileRequest;
    public override ServerKind Server => ServerKind.Femsg;

    public override Task HandleAsync(ProfileRequest request, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null || request.UserId != session.UserId.Value)
            throw new InvalidDataException("PROFILE_REQUEST for a different or unauthenticated user");

        // The retail client can open a gumonji.net profile URL after this request.
        // There is no in-protocol acknowledgement required before it continues.
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

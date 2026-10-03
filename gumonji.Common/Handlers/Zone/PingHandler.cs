using gumonji.Common.World;

namespace gumonji.Common.Handlers.Zone;

public sealed class PingHandler : PacketHandlerBase<PingRequest>
{
    public override PacketType RequestType => PacketType.PingRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override async Task HandleAsync(PingRequest request, GumonjiSession session, CancellationToken ct)
    {
        if (session.State == SessionState.ZoneEntered)
        {
            await session.SaveConditionAsync(ct: ct);
            if (session.Zone.StepAnimal())
            {
                // Continue roaming from the animal's latest client-reported
                // position instead of teleporting it back to spawn after a bump.
                await session.Zone.BroadcastAsync(new AnimalPlaceResponse(SpawnActors.CowId, session.CowX, session.CowY),
                    session.CowX * 1000u, session.CowY * 1000u, ct: ct);
            }
        }
        await session.SendAsync(new PingResponse(request.Echoed), ct);
    }
}

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
            if (session.PlantedChunks.Contains(((uint)session.CowX / PlantWorld.PageEdge,
                    (uint)session.CowY / PlantWorld.PageEdge)))
            {
                // Continue roaming from the animal's latest client-reported
                // position instead of teleporting it back to spawn after a bump.
                var step = Random.Shared.Next(4);
                session.CowX = (ushort)Math.Clamp(session.CowX + (step == 0 ? -1 : step == 1 ? 1 : 0), 0, 159);
                session.CowY = (ushort)Math.Clamp(session.CowY + (step == 2 ? -1 : step == 3 ? 1 : 0), 0, 159);
                await session.SendAsync(new AnimalPlaceResponse(SpawnActors.CowId, session.CowX, session.CowY), ct);
            }
        }
        await session.SendAsync(new PingResponse(request.Echoed), ct);
    }
}

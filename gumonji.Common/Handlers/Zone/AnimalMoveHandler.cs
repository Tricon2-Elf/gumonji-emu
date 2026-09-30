using gumonji.Common.World;
using gumonji.Network.Packets.Zone;

namespace gumonji.Common.Handlers.Zone;

public sealed class AnimalMoveHandler : PacketHandlerBase<AnimalMoveRequest>
{
    public override PacketType RequestType => PacketType.AnimalMoveRequest;
    public override ServerKind Server => ServerKind.Zone;

    public override Task HandleAsync(AnimalMoveRequest request, GumonjiSession session, CancellationToken ct)
    {
        session.EnsureZonePacket(RequestType);
        if (session.State != SessionState.ZoneEntered)
            throw new InvalidDataException("animal movement before zone entry");
        if (request.AnimalId != SpawnActors.CowId)
            return Task.CompletedTask;

        // sub_435780 sends this when the client moves an animal to an adjacent
        // cell, including after a collision. A fresh placement packet here
        // resets the client's in-progress jump; this is a one-way update.
        if (request.X < 160 && request.Y < 160)
        {
            session.CowX = request.X;
            session.CowY = request.Y;
        }
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

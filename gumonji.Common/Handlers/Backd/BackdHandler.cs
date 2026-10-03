using gumonji.Network;
using gumonji.Network.Packets.Backd;

namespace gumonji.Common.Handlers.Backd;

public abstract class BackdHandler<TRequest> : PacketHandlerBase<TRequest, BackdSession>
    where TRequest : IIncomingPacket<TRequest>
{
    public override ServerKind Server => ServerKind.Backd;
}

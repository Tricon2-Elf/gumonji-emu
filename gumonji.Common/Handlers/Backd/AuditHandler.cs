using gumonji.Network.Packets.Backd;
using gumonji.Network;
using Microsoft.Extensions.Logging;

namespace gumonji.Common.Handlers.Backd;

public sealed class AuditHandler(ILogger<AuditHandler> logger) : BackdHandler<AuditRequest>
{
    public override PacketType RequestType => PacketType.BackdAuditRequest;

    public override Task HandleAsync(AuditRequest request,
        BackdSession session, CancellationToken ct)
    {
        logger.LogInformation("backd audit zone={Zone} code={Code} uid={UserId} nameBytes={NameLength} messageBytes={MessageLength}",
            session.ZoneName, request.Code, request.UserId, request.Name.Length, request.Message.Length);
        return Task.CompletedTask;
    }
}

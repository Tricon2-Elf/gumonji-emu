namespace gumonji.Common.Handlers.Zone;

public sealed class EnvironmentReportHandler : PacketHandlerBase<EnvironmentReportRequest>
{
    public override PacketType RequestType => PacketType.EnvironmentReportRequest;
    public override ServerKind Server => ServerKind.Zone;
    public override Task HandleAsync(EnvironmentReportRequest request, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null) throw new InvalidDataException("environment report before authentication");
        session.LastEnvironmentReport = request;
        session.Zone.ReportEnvironment(session.UserId.Value, request);
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

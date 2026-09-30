namespace gumonji.Common.Handlers.Femsg;

public sealed class InventoryTemplateHandler : PacketHandlerBase<InventoryTemplateNotice>
{
    public override PacketType RequestType => PacketType.InventoryTemplateNotice;
    public override ServerKind Server => ServerKind.Femsg;

    public override Task HandleAsync(InventoryTemplateNotice request, GumonjiSession session, CancellationToken ct)
    {
        if (session.UserId is null || session.State != SessionState.HandoffIssued)
            throw new InvalidDataException("inventory template sync before zone handoff");
        session.LastInventoryTemplates = request;
        // The client sends this as a one-way frontend notification after a
        // local avatar update. There is no corresponding reply opcode.
        session.SilentNoReply = true;
        return Task.CompletedTask;
    }
}

namespace gumonji.Common;

public interface IPacketSession
{
    ServerKind Kind { get; }
    Task SendAsync(IOutgoingPacket packet, CancellationToken ct = default);

    // Gameplay handlers enforce their own state rules; service sessions can gate all requests.
    void ValidateRequest(PacketType type) { }
    bool HandleUnknownPacket(PacketType type) => false;
}

namespace gumonji.Common;

public sealed class BackdSession(Func<PacketType, byte[], CancellationToken, Task> send) : IPacketSession
{
    public ServerKind Kind => ServerKind.Backd;
    public Guid Id { get; } = Guid.NewGuid();
    public string? ZoneName { get; set; }
    public bool Authenticated => ZoneName is not null;

    public void ValidateRequest(PacketType type)
    {
        if (type != PacketType.BackdLoginRequest && !Authenticated)
            throw new InvalidDataException("backd message before frontend_login");
    }

    public bool HandleUnknownPacket(PacketType type) =>
        throw new InvalidDataException($"unsupported backd opcode {(uint)type}");

    public Task SendAsync(IOutgoingPacket packet, CancellationToken ct = default) =>
        send(packet.Type, packet.ToBytes(), ct);
}

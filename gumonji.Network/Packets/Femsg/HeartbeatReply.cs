namespace gumonji.Network.Packets.Femsg;

public sealed class HeartbeatReply(byte[] echoed) : IOutgoingPacket
{
    public PacketType Type => PacketType.HeartbeatReply;

    public byte[] ToBytes() => echoed;

    public static HeartbeatReply FromRequest(HeartbeatRequest request) =>
        new(request.Fields.AsSpan(0, 12).ToArray());
}

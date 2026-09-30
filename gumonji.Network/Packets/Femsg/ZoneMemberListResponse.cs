namespace gumonji.Network.Packets.Femsg;

/// <summary>Empty zone roster; acknowledges 0x198 so the client leaves its timed wait state.</summary>
public sealed class ZoneMemberListResponse : IOutgoingPacket
{
    public PacketType Type => PacketType.ZoneMemberListResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(0u);             // status
        writer.WriteCompactCount(0); // member names
        writer.WriteCompactCount(0); // member IDs
        return writer.ToBytes();
    }
}

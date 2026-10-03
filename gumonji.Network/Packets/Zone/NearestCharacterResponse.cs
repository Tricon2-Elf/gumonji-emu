namespace gumonji.Network.Packets.Zone;

public sealed class NearestCharacterResponse(uint status, uint userId) : IOutgoingPacket
{
    public PacketType Type => PacketType.NearestCharacterResponse;
    public byte[] ToBytes()
    {
        var writer = new PacketWriter(); writer.Write(status); writer.Write(userId);
        return writer.ToBytes();
    }
}

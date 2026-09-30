namespace gumonji.Network.Packets.Femsg;

public sealed class TutorialCompleteResponse(bool success) : IOutgoingPacket
{
    public PacketType Type => PacketType.TutorialCompleteResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(success ? 0u : 1u);
        return writer.ToBytes();
    }
}

namespace gumonji.Network.Packets.Femsg;

/// <summary>Clears the client's outstanding frontend progress value.</summary>
public sealed class ProgressValueResponse : IOutgoingPacket
{
    public PacketType Type => PacketType.ProgressValueResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(0u);
        return writer.ToBytes();
    }
}

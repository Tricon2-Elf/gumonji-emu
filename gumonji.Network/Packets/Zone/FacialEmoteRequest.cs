namespace gumonji.Network.Packets.Zone;

/// <summary>Client facial/emotion selection: one u32 emotion id.</summary>
public sealed class FacialEmoteRequest(uint emotionId) : IIncomingPacket<FacialEmoteRequest>
{
    public uint EmotionId { get; } = emotionId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(EmotionId);
        return writer.ToBytes();
    }

    public static FacialEmoteRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var emotionId = reader.ReadUInt32();
        reader.ExpectEnd();
        return new FacialEmoteRequest(emotionId);
    }
}

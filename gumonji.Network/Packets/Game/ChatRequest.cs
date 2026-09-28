namespace gumonji.Network.Packets.Game;

/// <summary>Client chat report: compact sender label, followed by compact message text.</summary>
public sealed class ChatRequest(byte[] sender, byte[] message) : IIncomingPacket<ChatRequest>
{
    public byte[] Sender { get; } = sender;
    public byte[] Message { get; } = message;

    public static ChatRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var sender = reader.ReadCompactBytes();
        var message = reader.ReadCompactBytes();
        if (sender.Length > 256 || message.Length > 1024)
            throw new InvalidDataException("invalid CHAT field lengths");
        reader.ExpectEnd();
        return new ChatRequest(sender, message);
    }
}

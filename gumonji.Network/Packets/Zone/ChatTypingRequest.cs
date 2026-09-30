namespace gumonji.Network.Packets.Zone;

/// <summary>Zero-payload typing notification sent for each printable key in a text field.</summary>
public sealed class ChatTypingRequest : IIncomingPacket<ChatTypingRequest>
{
    public static ChatTypingRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (!data.IsEmpty)
            throw new InvalidDataException("CHAT_TYPING must not contain a payload");
        return new ChatTypingRequest();
    }
}

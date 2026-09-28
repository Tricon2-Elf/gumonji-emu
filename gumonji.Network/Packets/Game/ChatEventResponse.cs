namespace gumonji.Network.Packets.Game;

/// <summary>
/// Server-to-client world-chat event. The client dispatches this as opcode 0x460
/// and renders the final compact field as the speech bubble/message text.
/// </summary>
public sealed class ChatEventResponse(uint entityId, byte[] sender, byte[] message) : IOutgoingPacket
{
    public PacketType Type => PacketType.ChatEventResponse;

    public byte[] ToBytes()
    {
        if (entityId is 0 or > 0x7FFFFFFF)
            throw new InvalidDataException("chat entity id must be a positive signed 32-bit value");
        if (sender.Length > 32 || message.Length > 1024)
            throw new InvalidDataException("chat sender or message is out of range");

        var writer = new PacketWriter();

        // Client dispatcher 0x43A91F compares opcode 0x460 and branches to
        // 0x43DA65, whose parser calls the chat renderer at 0x43DDCD.
        // Exact 0x460 client layout:
        // kind, actor ID, auxiliary ID, two style bytes, then three compact blobs.
        // This packet is already the client-facing chat event; it has no relay header.
        writer.Write(0u); // ordinary chat kind
        writer.Write(entityId); // actor ID used to locate the world entity
        writer.Write(0u); // auxiliary actor/chat ID
        writer.Write((byte)0); // name style
        writer.Write((byte)0); // message style
        writer.WriteCompactBytes(sender);
        writer.WriteCompactCount(0); // optional text/channel data
        writer.WriteCompactBytes(message);
        return writer.ToBytes();
    }
}

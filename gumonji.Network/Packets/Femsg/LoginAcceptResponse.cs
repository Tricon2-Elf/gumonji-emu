namespace gumonji.Network.Packets.Femsg;

public sealed class LoginAcceptResponse(uint userId, byte[] nickname) : IOutgoingPacket
{
    public PacketType Type => PacketType.LoginAcceptResponse;

    public LoginAcceptResponse(uint userId) : this(userId, "Local Player"u8.ToArray())
    {
    }

    public byte[] ToBytes()
    {
        if (userId is 0 or > 0x7FFFFFFF || nickname.Length > 128)
            throw new InvalidDataException("invalid local user ID or nickname");
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(userId);
        writer.WriteCompactBytes(nickname);
        writer.Write(100u);
        writer.Write(0u);
        writer.Write(0u);
        writer.WriteCompactBytes(new byte[100]);
        writer.Write(1u);
        return writer.ToBytes();
    }
}

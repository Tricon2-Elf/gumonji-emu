namespace gumonji.Network.Packets.Femsg;

public sealed class LoginAcceptResponse(uint userId, byte[] nickname, byte[] tutorialFlags) : IOutgoingPacket
{
    public PacketType Type => PacketType.LoginAcceptResponse;

    public LoginAcceptResponse(uint userId) : this(userId, "Local Player"u8.ToArray(), new byte[100])
    {
    }

    public LoginAcceptResponse(uint userId, byte[] tutorialFlags)
        : this(userId, "Local Player"u8.ToArray(), tutorialFlags)
    {
    }

    public byte[] ToBytes()
    {
        if (userId is 0 or > 0x7FFFFFFF || nickname.Length > 128 || tutorialFlags.Length != 100)
            throw new InvalidDataException("invalid local user ID or nickname");
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write(userId);
        writer.WriteCompactBytes(nickname);
        writer.Write(100u);
        writer.Write(0u);
        writer.Write(tutorialFlags.Any(flag => flag != 0) ? 1u : 0u);
        writer.WriteCompactBytes(tutorialFlags);
        writer.Write(1u);
        return writer.ToBytes();
    }
}

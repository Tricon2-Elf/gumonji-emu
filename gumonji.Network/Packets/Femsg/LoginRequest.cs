namespace gumonji.Network.Packets.Femsg;

public sealed class LoginRequest(byte[] username, byte[] password) : IIncomingPacket<LoginRequest>
{
    public byte[] Username { get; } = username;
    public byte[] Password { get; } = password;

    public static LoginRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var username = reader.ReadCompactBytes();
        var password = reader.ReadCompactBytes();
        if (username.Length > 128 || password.Length > 128 || reader.Remaining != 0)
            throw new InvalidDataException("invalid credential field lengths");
        return new LoginRequest(username, password);
    }
}

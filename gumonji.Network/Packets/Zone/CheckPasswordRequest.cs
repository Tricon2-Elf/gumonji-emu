namespace gumonji.Network.Packets.Zone;

public sealed class CheckPasswordRequest(uint userId, byte[] token) : IIncomingPacket<CheckPasswordRequest>
{
    public uint UserId { get; } = userId;
    public byte[] Token { get; } = token;

    public static CheckPasswordRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        if (reader.Remaining < 5)
            throw new InvalidDataException("truncated game CHECK_PASSWORD");
        var userId = reader.ReadUInt32();
        var token = reader.ReadCompactBytes();
        if (token.Length > 128 || reader.Remaining != 0)
            throw new InvalidDataException("invalid CHECK_PASSWORD token length");
        return new CheckPasswordRequest(userId, token);
    }
}

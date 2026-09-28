namespace gumonji.Network.Packets.Femsg;

public sealed class ZoneHandoffResponse(byte[] token, string gameIp, int gamePort) : IOutgoingPacket
{
    public PacketType Type => PacketType.ZoneHandoffResponse;

    public byte[] ToBytes()
    {
        if (token.Length is < 1 or > 127 || token.Contains((byte)0))
            throw new InvalidDataException("handoff token must fit the client's NUL-terminated buffer");
        if (gamePort is < 1 or > 0xFFFF)
            throw new InvalidDataException("game port must be between 1 and 65535");
        var packed = System.Net.IPAddress.Parse(gameIp).GetAddressBytes();
        var ipValue = (uint)(packed[0] | (packed[1] << 8) | (packed[2] << 16) | (packed[3] << 24));
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.WriteCompactBytes([.. token, 0]);
        writer.Write(ipValue);
        writer.Write((ushort)gamePort);
        return writer.ToBytes();
    }
}

namespace gumonji.Network.Packets.Game;

public sealed class CharacterCreateRequest(uint body, uint model, uint style, uint color, byte[] name)
    : IIncomingPacket<CharacterCreateRequest>
{
    public uint Body { get; } = body;
    public uint Model { get; } = model;
    public uint Style { get; } = style;
    public uint Color { get; } = color;
    public byte[] Name { get; } = name;

    public static CharacterCreateRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < 17)
            throw new InvalidDataException("truncated CHARACTER_CREATE");
        var reader = new PacketReader(data);
        var body = reader.ReadUInt32();
        var model = reader.ReadUInt32();
        var style = reader.ReadUInt32();
        var color = reader.ReadUInt32();
        var name = reader.ReadCompactBytes();
        if (reader.Remaining != 0 || name.Length > 128 || name.Contains((byte)0))
            throw new InvalidDataException("invalid CHARACTER_CREATE name");
        return new CharacterCreateRequest(body, model, style, color, name);
    }
}

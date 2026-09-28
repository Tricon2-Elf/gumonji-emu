namespace gumonji.Network.Packets.Game;

public sealed class CharacterFieldRequest(uint value) : IIncomingPacket<CharacterFieldRequest>
{
    public uint Value { get; } = value;

    public static CharacterFieldRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 4)
            throw new InvalidDataException("invalid CHARACTER_FIELD_REQUEST size");
        return new CharacterFieldRequest(new PacketReader(data).ReadUInt32());
    }
}

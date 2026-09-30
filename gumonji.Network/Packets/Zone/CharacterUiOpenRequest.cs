namespace gumonji.Network.Packets.Zone;

public sealed class CharacterUiOpenRequest : IIncomingPacket<CharacterUiOpenRequest>
{
    public static CharacterUiOpenRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
            throw new InvalidDataException("invalid CHARACTER_UI_OPEN size");
        return new CharacterUiOpenRequest();
    }
}

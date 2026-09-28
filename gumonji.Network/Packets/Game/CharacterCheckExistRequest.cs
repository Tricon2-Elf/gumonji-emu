namespace gumonji.Network.Packets.Game;

public sealed class CharacterCheckExistRequest : IIncomingPacket<CharacterCheckExistRequest>
{
    public static CharacterCheckExistRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
            throw new InvalidDataException("invalid or unauthenticated CHARACTER_CHECK_EXIST");
        return new CharacterCheckExistRequest();
    }
}

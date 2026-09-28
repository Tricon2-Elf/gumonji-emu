namespace gumonji.Network.Packets.Game;

/// <summary>Requests assignment of the already-created character for this account.</summary>
public sealed class CharacterLoadRequest : IIncomingPacket<CharacterLoadRequest>
{
    public static CharacterLoadRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
            throw new InvalidDataException("invalid CHARACTER_LOAD size");
        return new CharacterLoadRequest();
    }
}

namespace gumonji.Network.Packets.Game;

public sealed record CharacterConditionRequest : IIncomingPacket<CharacterConditionRequest>
{
    public static CharacterConditionRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        reader.ExpectEnd();
        return new();
    }
}

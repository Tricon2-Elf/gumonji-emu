namespace gumonji.Network.Packets.Zone;

public sealed record CharacterConditionRequest : IIncomingPacket<CharacterConditionRequest>
{
    public static CharacterConditionRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        reader.ExpectEnd();
        return new();
    }
}

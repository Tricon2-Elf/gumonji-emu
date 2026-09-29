namespace gumonji.Network.Packets.Game;

public sealed record MovementTotalsRequest(uint CharacterField, uint Walking, uint Swimming)
    : IIncomingPacket<MovementTotalsRequest>
{
    public static MovementTotalsRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var result = new MovementTotalsRequest(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());
        reader.ExpectEnd();
        return result;
    }
}

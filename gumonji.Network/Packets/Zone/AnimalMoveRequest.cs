namespace gumonji.Network.Packets.Zone;

// Client sender sub_450190: animal id, target cell and movement/action byte.
public sealed record AnimalMoveRequest(uint AnimalId, ushort X, ushort Y, byte Action)
    : IIncomingPacket<AnimalMoveRequest>
{
    public static AnimalMoveRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var request = new AnimalMoveRequest(reader.ReadUInt32(), reader.ReadUInt16(),
            reader.ReadUInt16(), reader.ReadByte());
        reader.ExpectEnd();
        return request;
    }
}

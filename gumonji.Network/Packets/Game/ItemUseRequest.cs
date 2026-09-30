namespace gumonji.Network.Packets.Game;

// Client sender sub_44D9D0 / opcode 0x077C.
public sealed record ItemUseRequest(uint Slot, uint TargetId, ushort X, ushort Y, uint Option)
    : IIncomingPacket<ItemUseRequest>
{
    public static ItemUseRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var request = new ItemUseRequest(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt16(),
            reader.ReadUInt16(), reader.ReadUInt32());
        reader.ExpectEnd();
        return request;
    }
}

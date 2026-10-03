namespace gumonji.Network.Packets.Zone;

public sealed class NearestCharacterRequest : IIncomingPacket<NearestCharacterRequest>
{
    public static NearestCharacterRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != 8) throw new InvalidDataException("invalid NEAREST_CHARACTER size");
        // Both u32 values are decoded but not passed to original sub_45D0A0.
        var reader = new PacketReader(data);
        _ = reader.ReadUInt32(); _ = reader.ReadUInt32();
        return new();
    }
}

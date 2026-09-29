namespace gumonji.Network.Packets.Game;

public sealed record PlantHarvestRequest(uint PlantId) : IIncomingPacket<PlantHarvestRequest>
{
    public static PlantHarvestRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var result = new PlantHarvestRequest(reader.ReadUInt32());
        reader.ExpectEnd();
        return result;
    }
}

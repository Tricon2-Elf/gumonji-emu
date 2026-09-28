namespace gumonji.Network.Packets.Game;

public sealed class PageDataResponse(uint chunkX, uint chunkY, ushort terrainId = 1) : IOutgoingPacket
{
    public const int Edge = 32;
    public const int Cells = Edge * Edge;

    public PacketType Type => PacketType.PageDataResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(chunkX);
        writer.Write(chunkY);
        writer.Write(0u);
        WriteTerrain(writer, terrainId);
        WriteZeros(writer, 4);
        WriteZeros(writer, 1);
        for (var i = 0; i < 4; i++)
            WriteZeros(writer, 4);
        return writer.ToBytes();
    }

    private static void WriteTerrain(PacketWriter writer, ushort terrainId)
    {
        writer.WriteCompactCount(Cells);
        var cell = new byte[2];
        cell[0] = (byte)(terrainId >> 8);
        cell[1] = (byte)terrainId;
        for (var i = 0; i < Cells; i++)
            writer.Write(cell);
    }

    private static void WriteZeros(PacketWriter writer, int bytesPerCell)
    {
        writer.WriteCompactCount(Cells);
        writer.Write(new byte[bytesPerCell * Cells]);
    }
}

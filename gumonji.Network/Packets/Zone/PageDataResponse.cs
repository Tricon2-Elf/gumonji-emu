namespace gumonji.Network.Packets.Zone;

public sealed class PageDataResponse : IOutgoingPacket
{
    public const int Edge = 32;
    public const int Cells = Edge * Edge;

    private readonly uint _chunkX;
    private readonly uint _chunkY;
    private readonly ushort[] _terrainIds;
    private readonly uint[] _heights;
    private readonly uint[] _waterLevels;

    public PageDataResponse(uint chunkX, uint chunkY, ushort terrainId = 1)
        : this(chunkX, chunkY, Enumerable.Repeat(terrainId, Cells).ToArray(), new uint[Cells], new uint[Cells]) { }

    public PageDataResponse(uint chunkX, uint chunkY, ushort[] terrainIds, uint[] heights, uint[] waterLevels)
    {
        if (terrainIds.Length != Cells || heights.Length != Cells || waterLevels.Length != Cells)
            throw new InvalidDataException($"terrain pages must contain exactly {Cells} cells");
        _chunkX = chunkX;
        _chunkY = chunkY;
        _terrainIds = terrainIds;
        _heights = heights;
        _waterLevels = waterLevels;
    }

    public PacketType Type => PacketType.PageDataResponse;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(_chunkX);
        writer.Write(_chunkY);
        writer.Write(0u);
        WriteTerrain(writer, _terrainIds);
        WriteUInts(writer, _heights);
        WriteZeros(writer, 1);
        WriteWaterLevels(writer, _waterLevels);
        for (var i = 0; i < 3; i++)
            WriteZeros(writer, 4);
        return writer.ToBytes();
    }

    private static void WriteTerrain(PacketWriter writer, ushort[] terrainIds)
    {
        writer.WriteCompactCount(Cells);
        foreach (var terrainId in terrainIds)
            writer.Write(terrainId);
    }

    private static void WriteUInts(PacketWriter writer, uint[] values)
    {
        writer.WriteCompactCount(Cells);
        foreach (var value in values)
            writer.Write(value);
    }

    private static void WriteWaterLevels(PacketWriter writer, uint[] levels)
    {
        writer.WriteCompactCount(Cells);
        foreach (var level in levels)
        {
            // Client's chunk loader stores water as (wire value - 1), and
            // treats zero as an empty cell. Preserve zero for dry terrain.
            writer.Write(level == 0 ? 0 : checked(level + 1));
        }
    }

    private static void WriteZeros(PacketWriter writer, int bytesPerCell)
    {
        writer.WriteCompactCount(Cells);
        writer.Write(new byte[bytesPerCell * Cells]);
    }
}

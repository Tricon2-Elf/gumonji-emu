namespace gumonji.Network.Packets.Backd;

internal static class BackdPacketFields
{
    public static byte[] ReadBytes(ref PacketReader reader, int limit)
    {
        var length = reader.ReadCompactLength();
        if (length > limit)
            throw new InvalidDataException($"backd field exceeds {limit} bytes");
        return reader.ReadBytes(length).ToArray();
    }

    public static uint[] ReadArray(ref PacketReader reader, int count)
    {
        var values = new uint[count];
        for (var index = 0; index < count; index++)
            values[index] = reader.ReadUInt32();
        return values;
    }

    public static uint[] ReadCompactArray(ref PacketReader reader, int maxCount)
    {
        var count = reader.ReadCompactLength();
        if (count > maxCount)
            throw new InvalidDataException($"backd array exceeds {maxCount} values");
        return ReadArray(ref reader, count);
    }

    public static void WriteArray(PacketWriter writer, uint[] values)
    {
        writer.WriteCompactCount(values.Length);
        foreach (var value in values) writer.Write(value);
    }
}

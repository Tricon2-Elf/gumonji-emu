namespace gumonji.Network.Packets.Zone;

public sealed record EnvironmentReportRequest(byte[] Os, byte[] Cpu, byte[] GraphicsCard,
    byte[] GraphicsDriver, uint VideoMemory, byte[] Timezone, uint MainMemory, uint LoopTest)
    : IIncomingPacket<EnvironmentReportRequest>
{
    public static EnvironmentReportRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var os = ReadField(ref reader); var cpu = ReadField(ref reader);
        var card = ReadField(ref reader); var driver = ReadField(ref reader);
        var vram = reader.ReadUInt32(); var timezone = ReadField(ref reader);
        var main = reader.ReadUInt32(); var loop = reader.ReadUInt32();
        reader.ExpectEnd();
        return new(os, cpu, card, driver, vram, timezone, main, loop);
    }

    private static byte[] ReadField(ref PacketReader reader)
    {
        var length = reader.ReadCompactLength();
        if (length > 128) throw new InvalidDataException("environment field exceeds 128 bytes");
        return reader.ReadBytes(length).ToArray();
    }
}

namespace gumonji.Network;

public interface IPacketWriter
{
    void Write(byte value);
    void Write(ushort value);
    void Write(uint value);
    void Write(ReadOnlySpan<byte> source);
    void WriteCompactCount(int count);
    void WriteCompactBytes(ReadOnlySpan<byte> value);
    byte[] ToBytes();
}

public interface IOutgoingPacket
{
    PacketType Type { get; }
    byte[] ToBytes();
}

public interface IIncomingPacket<TSelf>
    where TSelf : IIncomingPacket<TSelf>
{
    static abstract TSelf FromBytes(ReadOnlySpan<byte> data);
}

using System.Buffers.Binary;

namespace gumonji.Network;

/// <summary>Gumonji integers on the wire are big-endian.</summary>
public ref struct PacketReader(ReadOnlySpan<byte> buffer)
{
    private readonly ReadOnlySpan<byte> _buffer = buffer;
    private int _offset;

    public int Remaining => _buffer.Length - _offset;

    public byte ReadByte() => ReadSpan(1)[0];

    public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16BigEndian(ReadSpan(2));

    public uint ReadUInt32() => BinaryPrimitives.ReadUInt32BigEndian(ReadSpan(4));

    public ReadOnlySpan<byte> ReadBytes(int count) => ReadSpan(count);

    public byte[] ReadCompactBytes()
    {
        var length = ReadCompactLength();
        return ReadSpan(length).ToArray();
    }

    public int ReadCompactLength()
    {
        if (_offset >= _buffer.Length)
            throw new InvalidDataException("missing VCE string length");
        var marker = _buffer[_offset++];
        if (marker <= 0xFC)
            return marker;
        if (marker != 0xFD)
            throw new InvalidDataException($"unsupported VCE string marker 0x{marker:X2}");
        if (Remaining < 4)
            throw new InvalidDataException("truncated extended VCE string length");
        var size = ReadUInt32();
        if (size > int.MaxValue)
            throw new InvalidDataException("VCE string too large");
        return (int)size;
    }

    public void ExpectEnd()
    {
        if (Remaining != 0)
            throw new InvalidDataException("unexpected trailing packet bytes");
    }

    private ReadOnlySpan<byte> ReadSpan(int length)
    {
        if (length < 0 || _offset + length > _buffer.Length)
            throw new InvalidDataException(
                $"PacketReader: tried to read {length} bytes with {_buffer.Length - _offset} remaining");
        var span = _buffer.Slice(_offset, length);
        _offset += length;
        return span;
    }
}

public sealed class PacketWriter : IPacketWriter
{
    private readonly MemoryStream _stream = new();

    public byte[] ToBytes() => _stream.ToArray();

    public void Write(byte value) => _stream.WriteByte(value);

    public void Write(ushort value)
    {
        Span<byte> span = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(span, value);
        _stream.Write(span);
    }

    public void Write(uint value)
    {
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(span, value);
        _stream.Write(span);
    }

    public void Write(ReadOnlySpan<byte> source) => _stream.Write(source);

    public void WriteCompactCount(int count)
    {
        if (count < 0)
            throw new InvalidDataException("compact count out of range");
        if (count <= 0xFC)
        {
            _stream.WriteByte((byte)count);
            return;
        }
        _stream.WriteByte(0xFD);
        Write((uint)count);
    }

    public void WriteCompactBytes(ReadOnlySpan<byte> value)
    {
        WriteCompactCount(value.Length);
        _stream.Write(value);
    }
}

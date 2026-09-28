using System.Buffers.Binary;
using System.IO.Compression;
using System.Numerics;
using gumonji.Network.Crypto;

namespace gumonji.Network;

public static class VceLimits
{
    public const int Block = 16;
    public const int MaxRecordData = 8136;
    public const int MaxInflated = 0x100000;
    public const int MaxFrame = 4 * 1024 * 1024;
    public const int MaxEncryptedRecord = 0x1FF8;
}

public static class VceCompression
{
    /// <summary>sub_5BE630. The "gzip" magic is not a gzip member.</summary>
    public static byte[] Wrap(ReadOnlySpan<byte> plain)
    {
        if (plain.Length is <= 0 or >= VceLimits.MaxInflated)
            throw new InvalidDataException("invalid game compression input size");
        var compressed = Zlib(plain);
        var body = compressed.Length + 16 >= plain.Length ? plain.ToArray() : compressed;
        var envelope = new byte[16 + body.Length];
        "gzip"u8.CopyTo(envelope);
        BinaryPrimitives.WriteUInt32LittleEndian(envelope.AsSpan(4), (uint)plain.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(envelope.AsSpan(8), (uint)body.Length);
        "lv6\0"u8.CopyTo(envelope.AsSpan(12));
        body.CopyTo(envelope.AsSpan(16));
        return envelope;
    }

    public static byte[] Unwrap(ReadOnlySpan<byte> envelope)
    {
        if (envelope.Length < 16
            || !envelope[..4].SequenceEqual("gzip"u8)
            || !envelope.Slice(12, 4).SequenceEqual("lv6\0"u8))
            throw new InvalidDataException("invalid game VCE compression envelope (expected gzip/lv6)");
        var original = BinaryPrimitives.ReadUInt32LittleEndian(envelope[4..]);
        var stored = BinaryPrimitives.ReadUInt32LittleEndian(envelope[8..]);
        if (original is 0 || original >= VceLimits.MaxInflated)
            throw new InvalidDataException($"invalid game VCE uncompressed size {original}");
        if (stored != (uint)(envelope.Length - 16) || stored is 0 || stored > original)
            throw new InvalidDataException("invalid game VCE stored size");
        var body = envelope[16..];
        if (stored == original)
            return body.ToArray();
        try
        {
            using var input = new MemoryStream(body.ToArray());
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            if (output.Length != original || input.Position != input.Length)
                throw new InvalidDataException("game VCE zlib stream length/end mismatch");
            return output.ToArray();
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException("invalid game VCE zlib data", ex);
        }
    }

    private static byte[] Zlib(ReadOnlySpan<byte> plain)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(plain);
        return output.ToArray();
    }
}

public static class VceRecords
{
    public static async Task<byte[]> ReadAsync(Stream stream, Aes128Ecb cipher, bool compression, CancellationToken ct)
    {
        var header = await ReadExactAsync(stream, 8, ct);
        var encryptedSize = BinaryPrimitives.ReadInt32BigEndian(header);
        var plainSize = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(4));
        if (encryptedSize > VceLimits.MaxEncryptedRecord || encryptedSize % 16 != 0 || plainSize > encryptedSize || plainSize < 0)
            throw new InvalidDataException($"invalid VCE record header: {Convert.ToHexString(header)}");
        var encrypted = await ReadExactAsync(stream, encryptedSize, ct);
        var decrypted = cipher.Decrypt(encrypted);
        var plain = decrypted.AsSpan(0, plainSize).ToArray();
        return compression ? VceCompression.Unwrap(plain) : plain;
    }

    public static async Task WriteAsync(Stream stream, Aes128Ecb cipher, ReadOnlyMemory<byte> plain, bool compression, CancellationToken ct)
    {
        if (plain.Length == 0)
            throw new InvalidDataException("cannot send an empty VCE record");
        for (var pos = 0; pos < plain.Length; pos += VceLimits.MaxRecordData)
        {
            var slice = plain.Slice(pos, Math.Min(VceLimits.MaxRecordData, plain.Length - pos)).ToArray();
            var encoded = compression ? VceCompression.Wrap(slice) : slice;
            var paddedSize = (encoded.Length / VceLimits.Block + 1) * VceLimits.Block;
            var padded = new byte[paddedSize];
            encoded.CopyTo(padded, 0);
            var encrypted = cipher.Encrypt(padded);
            var header = new byte[8];
            BinaryPrimitives.WriteInt32BigEndian(header, encrypted.Length);
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), encoded.Length);
            await stream.WriteAsync(header, ct);
            await stream.WriteAsync(encrypted, ct);
            await stream.FlushAsync(ct);
        }
    }

    public static async Task<byte[]> ReadExactAsync(Stream stream, int size, CancellationToken ct)
    {
        var buffer = new byte[size];
        var offset = 0;
        while (offset < size)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, size - offset), ct);
            if (read == 0)
                throw new EndOfStreamException($"disconnected during read ({offset}/{size} bytes)");
            offset += read;
        }
        return buffer;
    }
}

public sealed class InnerFrames
{
    private byte[] _pending = [];

    public List<byte[]> Feed(ReadOnlySpan<byte> data)
    {
        if (data.Length > 0)
        {
            var grown = new byte[_pending.Length + data.Length];
            _pending.CopyTo(grown, 0);
            data.CopyTo(grown.AsSpan(_pending.Length));
            _pending = grown;
        }

        var frames = new List<byte[]>();
        var offset = 0;
        while (_pending.Length - offset >= 4)
        {
            var size = BinaryPrimitives.ReadUInt32BigEndian(_pending.AsSpan(offset));
            if (size > VceLimits.MaxFrame)
                throw new InvalidDataException($"invalid inner frame length {size}; check transport envelope/cipher");
            if (_pending.Length - offset < size + 4)
                break;
            frames.Add(_pending.AsSpan(offset + 4, (int)size).ToArray());
            offset += 4 + (int)size;
        }
        if (offset > 0)
            _pending = _pending[offset..];
        return frames;
    }
}

public static class DhHandshake
{
    public static async Task<Aes128Ecb> AcceptAsync(Stream stream, CancellationToken ct)
    {
        var hello = await VceRecords.ReadExactAsync(stream, 8, ct);
        var version = BinaryPrimitives.ReadUInt32BigEndian(hello);
        var keySize = BinaryPrimitives.ReadUInt32BigEndian(hello.AsSpan(4));
        if (version != 1 || keySize != 16)
            throw new InvalidDataException($"unsupported legacy VCE hello: version={version}, key_bytes={keySize}");

        var privateKey = LegacyDh.RandomPrivate();
        var publicKey = BigInteger.ModPow(2, privateKey, LegacyDh.Prime);
        var reply = BuildReply(publicKey);
        await stream.WriteAsync(reply, ct);
        await stream.FlushAsync(ct);

        var lengthBytes = await VceRecords.ReadExactAsync(stream, 4, ct);
        var size = BinaryPrimitives.ReadInt32BigEndian(lengthBytes);
        if (size is < 1 or > 1022)
            throw new InvalidDataException($"invalid DH public-key text length {size}");
        var encoded = await VceRecords.ReadExactAsync(stream, size, ct);
        if (encoded.Any(c => !IsHex(c)))
            throw new InvalidDataException("DH public key is not ASCII hexadecimal");
        var peer = LegacyDh.ParseUnsignedHex(System.Text.Encoding.ASCII.GetString(encoded));
        if (peer <= 1 || peer >= LegacyDh.Prime - 1)
            throw new InvalidDataException("DH public key outside expected range");
        var shared = BigInteger.ModPow(peer, privateKey, LegacyDh.Prime);
        return new Aes128Ecb(LegacyDh.DeriveAesKey(shared));
    }

    private static byte[] BuildReply(BigInteger publicKey)
    {
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.WriteCompactField("2"u8);
        writer.WriteCompactField(System.Text.Encoding.ASCII.GetBytes(LegacyDh.PrimeHex));
        writer.WriteCompactField(System.Text.Encoding.ASCII.GetBytes(LegacyDh.BnHex(publicKey)));
        return writer.ToBytes();
    }

    private static bool IsHex(byte c) => c is (>= (byte)'0' and <= (byte)'9') or (>= (byte)'a' and <= (byte)'f') or (>= (byte)'A' and <= (byte)'F');
}

file static class PacketWriterFields
{
    public static void WriteCompactField(this PacketWriter writer, ReadOnlySpan<byte> data)
    {
        writer.Write((uint)data.Length);
        writer.Write(data);
    }
}

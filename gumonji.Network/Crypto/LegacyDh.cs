using System.Numerics;
using System.Security.Cryptography;

namespace gumonji.Network.Crypto;

/// <summary>
/// Legacy VCE 1.x Diffie-Hellman group and the client's non-standard AES key map.
/// sub_5A8510 keeps the first 16 bytes of the shared secret's hex, then
/// sub_5C7640 folds each ASCII nibble with <c>character &amp; 15</c>.
/// </summary>
public static class LegacyDh
{
    public const string PrimeHex =
        "f488fd584e49dbcd20b49de49107366b336c380d451d0f7c88b31c7c5b2d8ef6"
        + "f3c923c043f0a55b188d8ebb558cb85d38d334fd7c175743a31d186cde33212cb"
        + "52aff3ce1b1294018118d7c84a70a72d686c40319c807297aca950cd9969fabd"
        + "00a509b0246d3083d66a45d419f9c7cbd894b221926baaba25ec355e92f78c7";

    public static readonly BigInteger Prime = ParseUnsignedHex(PrimeHex);

    /// <summary>
    /// Hex from the wire is an unsigned integer. <see cref="BigInteger.Parse(string, System.Globalization.NumberStyles)"/>
    /// treats a high bit as a sign, which would reject a public key that starts with 8..F.
    /// </summary>
    public static BigInteger ParseUnsignedHex(string hex)
    {
        if (hex.Length % 2 == 1)
            hex = "0" + hex;
        return new BigInteger(Convert.FromHexString(hex), isUnsigned: true, isBigEndian: true);
    }

    /// <summary>sub_5CE100: big-endian hex with no leading zero bytes.</summary>
    public static string BnHex(BigInteger value)
    {
        if (value.Sign < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (bytes.Length == 0)
            bytes = [0];
        return Convert.ToHexString(bytes);
    }

    public static byte[] DeriveAesKey(BigInteger shared)
    {
        var raw = Convert.FromHexString(BnHex(shared));
        if (raw.Length < 16)
            throw new InvalidDataException("DH shared secret is too short");
        var digits = Convert.ToHexString(raw.AsSpan(0, 16)).ToLowerInvariant();
        var key = new byte[16];
        for (var i = 0; i < 16; i++)
            key[i] = (byte)(((digits[i * 2] & 15) << 4) | (digits[(i * 2) + 1] & 15));
        return key;
    }

    public static BigInteger RandomPrivate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        bytes[0] |= 0x80;
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
    }
}

public sealed class Aes128Ecb : IDisposable
{
    private readonly Aes _aes;

    public Aes128Ecb(byte[] key)
    {
        if (key.Length != 16)
            throw new ArgumentException("AES-128 requires a 16-byte key", nameof(key));
        _aes = Aes.Create();
        _aes.Mode = CipherMode.ECB;
        _aes.Padding = PaddingMode.None;
        _aes.Key = key;
    }

    public byte[] Encrypt(byte[] blockAligned) =>
        _aes.CreateEncryptor().TransformFinalBlock(blockAligned, 0, blockAligned.Length);

    public byte[] Decrypt(byte[] blockAligned) =>
        _aes.CreateDecryptor().TransformFinalBlock(blockAligned, 0, blockAligned.Length);

    public void Dispose() => _aes.Dispose();
}

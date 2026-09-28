using System.Security.Cryptography;

namespace gumonji.Common.Security;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static byte[] Hash(ReadOnlySpan<byte> password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var derived = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
        return [.. salt, .. derived];
    }

    public static bool Verify(ReadOnlySpan<byte> password, ReadOnlySpan<byte> stored)
    {
        if (stored.Length != SaltSize + HashSize)
            return false;

        var salt = stored[..SaltSize];
        var expected = stored[SaltSize..];
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

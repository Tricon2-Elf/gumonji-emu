using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace gumonji.Common.Accounts;

public sealed record Character(byte[] Name, int Body, int Model, int Style, int Color);

public sealed class LocalAccounts
{
    private readonly Dictionary<string, uint> _users = [];
    private readonly Dictionary<string, (uint UserId, long Expiry)> _tokens = [];

    public Dictionary<uint, Character> Characters { get; } = [];

    public uint Login(byte[] username)
    {
        var key = Convert.ToHexString(username);
        if (!_users.TryGetValue(key, out var id))
        {
            id = (uint)_users.Count + 1;
            _users[key] = id;
        }
        return id;
    }

    public byte[] Issue(uint userId, string? otpOverride)
    {
        var token = string.IsNullOrEmpty(otpOverride)
            ? Convert.ToHexString(RandomNumberGenerator.GetBytes(10)).ToLowerInvariant()
            : otpOverride;
        var raw = Encoding.ASCII.GetBytes(token);
        if (raw.Length is < 1 or > 127 || raw.Contains((byte)0))
            throw new InvalidDataException("OTP must contain 1..127 non-NUL ASCII bytes");
        var now = Stopwatch.GetTimestamp();
        foreach (var expired in _tokens.Where(pair => pair.Value.Expiry <= now).Select(pair => pair.Key).ToArray())
            _tokens.Remove(expired);
        _tokens[token] = (userId, now + (long)(120 * Stopwatch.Frequency));
        return raw;
    }

    public bool Consume(uint userId, byte[] token)
    {
        var key = Encoding.ASCII.GetString(token);
        if (!_tokens.TryGetValue(key, out var entry) || entry.UserId != userId || entry.Expiry <= Stopwatch.GetTimestamp())
            return false;
        _tokens.Remove(key);
        return true;
    }
}

using System.Buffers.Binary;
using System.Collections.Concurrent;
using gumonji.Common.DAL;
using gumonji.Common.DAL.Entities;
using gumonji.Common.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace gumonji.Common;

/// <summary>Shared backend state and persistence used by packet handlers.</summary>
public sealed class BackdState(IDbContextFactory<MainContext> dbFactory, IBackdCharacterRepository? characters = null)
{
    private readonly IBackdCharacterRepository _characters = characters ?? new BackdCharacterRepository(dbFactory);
    private readonly ConcurrentDictionary<uint, Guid> _locks = new();
    private readonly ConcurrentDictionary<uint, string> _onlineUsers = new();
    private readonly SemaphoreSlim _doorSequenceLock = new(1, 1);
    private readonly ILoginTokenRepository _loginTokens = new LoginTokenRepository(dbFactory);

    public void Disconnect(BackdSession session)
    {
        foreach (var (uid, owner) in _locks)
            if (owner == session.Id)
                _locks.TryRemove(new KeyValuePair<uint, Guid>(uid, owner));
    }

    public bool GetLock(uint uid, Guid sessionId) =>
        _locks.TryAdd(uid, sessionId) ||
        (_locks.TryGetValue(uid, out var owner) && owner == sessionId);

    public bool PutLock(uint uid, Guid sessionId) =>
        _locks.TryRemove(new KeyValuePair<uint, Guid>(uid, sessionId));

    public bool UserOnline(uint uid, string zone)
    {
        var alreadyOnline = !_onlineUsers.TryAdd(uid, zone);
        if (alreadyOnline) _onlineUsers[uid] = zone;
        return alreadyOnline;
    }

    public bool UserOffline(uint uid, string zone) =>
        _onlineUsers.TryRemove(new KeyValuePair<uint, string>(uid, zone));

    public async Task<(uint Result, byte[] Username)> CheckLoginAsync(uint uid, byte[] token, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == (long)uid, ct);
        if (user is null) return (unchecked((uint)-43), []);
        if (user.Username.Length > 64 || token.Length == 0 ||
            !await _loginTokens.ConsumeAsync(uid, token, ct))
            return (unchecked((uint)-3), []);
        return (0u, user.Username);
    }

    public async Task SaveCharacterAsync(uint uid, byte[] payload, CancellationToken ct)
    {
        var character = Backd.BackdCharacterCodec.Decode(uid, payload);
        await _characters.SaveAsync(character, ct);
    }

    public async Task<BackdCharacter?> GetCharacterAsync(uint uid, CancellationToken ct)
    {
        return await _characters.GetByUserIdAsync(uid, ct);
    }

    public async Task SaveHistoryAsync(uint[] values, CancellationToken ct)
    {
        var payload = new byte[24 * sizeof(uint)];
        for (var index = 0; index < values.Length; index++)
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(index * sizeof(uint)), values[index]);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.BackdHistories.FindAsync([(long)values[0]], ct);
        if (existing is null)
            db.BackdHistories.Add(new BackdHistory { UserId = values[0], Payload = payload });
        else
        {
            existing.Payload = payload;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<uint[]> LoadHistoryAsync(uint uid, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.BackdHistories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == (long)uid, ct);
        var values = new uint[24];
        values[0] = uid;
        if (row is null) return values;
        if (row.Payload.Length != values.Length * sizeof(uint))
            throw new InvalidDataException("stored backd history must contain exactly 24 integers");
        for (var index = 0; index < values.Length; index++)
            values[index] = BinaryPrimitives.ReadUInt32BigEndian(row.Payload.AsSpan(index * sizeof(uint)));
        return values;
    }

    public async Task<uint[]> AllocateDoorIdsAsync(uint requested, CancellationToken ct)
    {
        if (requested is 0 or > 10)
            throw new InvalidDataException("backd door ID batch must contain 1–10 IDs");
        await _doorSequenceLock.WaitAsync(ct);
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var sequence = await db.BackdSequences.FindAsync(["door"], ct);
            if (sequence is null)
            {
                sequence = new BackdSequence { Name = "door" };
                db.BackdSequences.Add(sequence);
            }
            if (sequence.NextId < 1 || sequence.NextId + requested - 1 > uint.MaxValue)
                throw new InvalidOperationException("backd door ID space exhausted");
            var ids = Enumerable.Range(0, (int)requested)
                .Select(offset => checked((uint)(sequence.NextId + offset))).ToArray();
            sequence.NextId += requested;
            await db.SaveChangesAsync(ct);
            return ids;
        }
        finally { _doorSequenceLock.Release(); }
    }
}

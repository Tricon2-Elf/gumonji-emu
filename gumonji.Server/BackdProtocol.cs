using System.Collections.Concurrent;
using System.Buffers.Binary;
using System.Text;
using gumonji.Common;
using gumonji.Common.DAL;
using gumonji.Common.DAL.Entities;
using gumonji.Common.DAL.Repositories;
using gumonji.Network;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace gumonji.Server;

public sealed class BackdSession
{
    public Guid Id { get; } = Guid.NewGuid();
    public string? ZoneName { get; set; }
    public bool Authenticated => ZoneName is not null;
}

/// <summary>Original zonesv/backend messages documented in docs/zonesv_backd_protocol.md.</summary>
public sealed class BackdProtocol(
    EmuOptions options,
    IDbContextFactory<MainContext> dbFactory,
    ILogger<BackdProtocol> logger)
{
    private const uint NoCharacter = unchecked((uint)-13);
    private const uint LockDenied = unchecked((uint)-36);
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

    public async Task HandleAsync(
        BackdSession session,
        ushort opcode,
        ReadOnlyMemory<byte> body,
        Func<ushort, byte[], CancellationToken, Task> send,
        CancellationToken ct = default)
    {
        // PacketReader is a ref struct, so finish all parsing before awaiting I/O.
        var message = Parse(opcode, body.Span);
        if (opcode == 1)
        {
            var name = Encoding.ASCII.GetString(message.Bytes0);
            var accepted = message.Bytes0.Length is > 0 and <= 16 &&
                (options.BackdPassword is null ||
                 Encoding.UTF8.GetBytes(options.BackdPassword).AsSpan().SequenceEqual(message.Bytes1));
            if (accepted)
                session.ZoneName = name;
            var reply = new PacketWriter();
            reply.Write(accepted ? 0u : unchecked((uint)-7));
            reply.Write(0u);
            reply.WriteCompactBytes([]);
            reply.Write(1u);
            await send(2, reply.ToBytes(), ct);
            logger.LogInformation("backd login zone={Zone} accepted={Accepted}", name, accepted);
            return;
        }
        if (!session.Authenticated)
            throw new InvalidDataException("backd message before frontend_login");

        // These are notifications. This zonesv build has no reply decoder for
        // them, so sending a made-up acknowledgement would violate the protocol.
        switch (opcode)
        {
            case 1301:
                var alreadyOnline = !_onlineUsers.TryAdd(message.Value0, session.ZoneName!);
                if (alreadyOnline)
                    _onlineUsers[message.Value0] = session.ZoneName!;
                logger.LogInformation("backd user online zone={Zone} uid={UserId} alreadyOnline={AlreadyOnline}",
                    session.ZoneName, message.Value0, alreadyOnline);
                return;
            case 1302:
                var removed = _onlineUsers.TryRemove(new KeyValuePair<uint, string>(message.Value0, session.ZoneName!));
                logger.LogInformation("backd user offline zone={Zone} uid={UserId} wasOnline={WasOnline}",
                    session.ZoneName, message.Value0, removed);
                return;
            case 1701:
                logger.LogInformation("backd seller IDs zone={Zone} count={Count}",
                    session.ZoneName, message.Array.Length);
                return;
            case 2001:
                logger.LogInformation(
                    "backd audit zone={Zone} code={Code} uid={UserId} nameBytes={NameLength} messageBytes={MessageLength}",
                    session.ZoneName, message.Value0, message.Value1,
                    message.Bytes0.Length, message.Bytes1.Length);
                return;
        }

        var writer = new PacketWriter();
        ushort replyId;
        switch (opcode)
        {
            case 107: // Validate the one-time token issued by the frontend.
                replyId = 108;
                var (loginResult, username) = await CheckLoginAsync(message.Value1, message.Bytes0, ct);
                writer.Write(message.Value0); // msgid
                writer.Write(loginResult);
                writer.Write(message.Value1); // uid
                writer.Write(0u); // reserved account value
                writer.Write(loginResult == 0 ? 1u : 0u); // account approval state
                writer.WriteCompactBytes(username);
                writer.WriteCompactBytes([]); // optional per-character permissions
                // zonesv stores this as the player's land-edit entitlement.
                // Zero makes get_edittype reject every planting/terrain action.
                writer.Write((ushort)(loginResult == 0 ? 1 : 0));
                break;
            case 5: // Status/version probe.
                replyId = 6;
                writer.Write(10001u);
                writer.Write(184022225u);
                writer.Write(0u);
                break;
            case 201: // Get character lock.
                replyId = 202;
                writer.Write(message.Value0);
                var acquired = _locks.TryAdd(message.Value1, session.Id) ||
                    (_locks.TryGetValue(message.Value1, out var owner) && owner == session.Id);
                writer.Write(acquired ? 0u : LockDenied);
                writer.Write(message.Value1);
                break;
            case 203: // Release character lock.
                replyId = 204;
                writer.Write(message.Value0);
                var released = _locks.TryRemove(new KeyValuePair<uint, Guid>(message.Value1, session.Id));
                writer.Write(released ? 0u : LockDenied);
                writer.Write(message.Value1);
                break;
            case 501: // Store exactly the character blob emitted by zonesv.
                replyId = 502;
                writer.Write(message.Value0);
                if (message.Value2 is (uint)'I' or (uint)'C' or (uint)'U')
                {
                    await SaveCharacterAsync(message.Value1, message.Bytes0, ct);
                    writer.Write(0u);
                }
                else
                    writer.Write(unchecked((uint)-6));
                writer.Write(message.Value2);
                break;
            case 503: // Load character, echoing the request's options array.
                replyId = 504;
                var character = await GetCharacterAsync(message.Value1, ct);
                writer.Write(message.Value0);
                writer.Write(message.Value1);
                writer.Write(character is null ? NoCharacter : 0u);
                writer.WriteCompactBytes(character?.Payload ?? []);
                writer.WriteCompactCount(message.Array.Length);
                foreach (var value in message.Array)
                    writer.Write(value);
                break;
            case 507: // Existence query. No callback is registered by this zonesv build.
                replyId = 508;
                writer.Write(message.Value0);
                writer.Write(await GetCharacterAsync(message.Value1, ct) is null ? NoCharacter : 0u);
                break;
            case 1501: // Request a batch of globally unique door IDs.
                replyId = 1502;
                var doorIds = await AllocateDoorIdsAsync(message.Value0, ct);
                writer.WriteCompactCount(doorIds.Length);
                foreach (var id in doorIds)
                    writer.Write(id);
                break;
            case 1702: // Player's vending-machine zone list.
                replyId = 1703;
                writer.Write(message.Value0); // msgid
                writer.Write(0u); // no seller UIDs until vending machines are modeled
                writer.WriteCompactCount(0); // no vendor/zone records
                break;
            case 2101: // Store the zone's 24-field user history record.
                replyId = 2102;
                await SaveHistoryAsync(message.Array, ct);
                writer.Write(0u);
                break;
            case 2111: // Load user history; the zone reads all 24 fields unconditionally.
                replyId = 2112;
                var history = await LoadHistoryAsync(message.Value1, ct);
                writer.Write(message.Value0); // msgid
                writer.Write(message.Value1); // uid
                writer.WriteCompactCount(history.Length);
                foreach (var value in history)
                    writer.Write(value);
                break;
            case 2409: // Query inter-zone passage links for this zone and its dimensions.
                replyId = 2410;
                writer.Write(message.Value0);
                // The zone callback reads six entries from every array regardless of
                // the encoded count. Zeroed entries mean no configured passages.
                for (var array = 0; array < 8; array++)
                {
                    writer.WriteCompactCount(6);
                    for (var index = 0; index < 6; index++)
                        writer.Write(0u);
                }
                break;
            default:
                throw new InvalidDataException($"unsupported backd opcode {opcode}");
        }
        await send(replyId, writer.ToBytes(), ct);
    }

    private async Task SaveCharacterAsync(uint uid, byte[] payload, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.BackdCharacters.FindAsync([(long)uid], ct);
        if (existing is null)
            db.BackdCharacters.Add(new BackdCharacter { UserId = uid, Payload = payload });
        else
        {
            existing.Payload = payload;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<BackdCharacter?> GetCharacterAsync(uint uid, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.BackdCharacters.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == uid, ct);
    }

    private async Task<(uint Result, byte[] Username)> CheckLoginAsync(uint uid, byte[] token, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == (long)uid, ct);
        if (user is null)
            return (unchecked((uint)-43), []); // not resident
        if (user.Username.Length > 64 || token.Length == 0 ||
            !await _loginTokens.ConsumeAsync(uid, token, ct))
            return (unchecked((uint)-3), []); // incorrect password/token
        return (0u, user.Username);
    }

    private async Task SaveHistoryAsync(uint[] values, CancellationToken ct)
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

    private async Task<uint[]> LoadHistoryAsync(uint uid, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.BackdHistories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == (long)uid, ct);
        var values = new uint[24];
        values[0] = uid;
        if (row is null)
            return values;
        if (row.Payload.Length != values.Length * sizeof(uint))
            throw new InvalidDataException("stored backd history must contain exactly 24 integers");
        for (var index = 0; index < values.Length; index++)
            values[index] = BinaryPrimitives.ReadUInt32BigEndian(row.Payload.AsSpan(index * sizeof(uint)));
        return values;
    }

    private async Task<uint[]> AllocateDoorIdsAsync(uint requested, CancellationToken ct)
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
        finally
        {
            _doorSequenceLock.Release();
        }
    }

    private static Message Parse(ushort opcode, ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var message = new Message();
        switch (opcode)
        {
            case 1:
                message.Bytes0 = ReadLimitedBytes(ref reader, 16);
                message.Bytes1 = ReadLimitedBytes(ref reader, 128);
                message.Value0 = reader.ReadUInt16();
                message.Value1 = reader.ReadUInt32();
                break;
            case 5:
                message.Value0 = reader.ReadUInt32();
                message.Value1 = reader.ReadUInt32();
                message.Value2 = reader.ReadUInt32();
                message.Value3 = reader.ReadUInt32();
                break;
            case 107:
                message.Value0 = reader.ReadUInt32();
                message.Value1 = reader.ReadUInt32();
                message.Bytes0 = ReadLimitedBytes(ref reader, 128);
                break;
            case 201 or 203 or 507 or 1702 or 2111:
                message.Value0 = reader.ReadUInt32();
                message.Value1 = reader.ReadUInt32();
                break;
            case 2101:
                message.Array = new uint[24];
                for (var i = 0; i < message.Array.Length; i++)
                    message.Array[i] = reader.ReadUInt32();
                break;
            case 501:
                message.Value0 = reader.ReadUInt32();
                message.Value1 = reader.ReadUInt32();
                message.Bytes0 = ReadLimitedBytes(ref reader, 262144);
                message.Value2 = reader.ReadUInt32();
                break;
            case 503:
                message.Value0 = reader.ReadUInt32();
                message.Value1 = reader.ReadUInt32();
                var count = reader.ReadCompactLength();
                if (count > 2)
                    throw new InvalidDataException("backd load options exceed two values");
                message.Array = new uint[count];
                for (var i = 0; i < count; i++)
                    message.Array[i] = reader.ReadUInt32();
                break;
            case 1501:
                message.Value0 = reader.ReadUInt32();
                break;
            case 2409:
                message.Value0 = reader.ReadUInt32();
                message.Value1 = reader.ReadUInt32();
                message.Value2 = reader.ReadUInt32();
                break;
            case 1301 or 1302:
                message.Value0 = reader.ReadUInt32();
                break;
            case 1701:
                var sellerCount = reader.ReadCompactLength();
                if (sellerCount > 2048)
                    throw new InvalidDataException("backd seller list exceeds 2048 IDs");
                message.Array = new uint[sellerCount];
                for (var i = 0; i < sellerCount; i++)
                    message.Array[i] = reader.ReadUInt32();
                break;
            case 2001:
                message.Value0 = reader.ReadUInt32();
                message.Value1 = reader.ReadUInt32();
                message.Bytes0 = ReadLimitedBytes(ref reader, 128);
                message.Bytes1 = ReadLimitedBytes(ref reader, 4096);
                break;
            default:
                throw new InvalidDataException($"unsupported backd opcode {opcode}");
        }
        reader.ExpectEnd();
        return message;
    }

    private static byte[] ReadLimitedBytes(ref PacketReader reader, int limit)
    {
        var length = reader.ReadCompactLength();
        if (length > limit)
            throw new InvalidDataException($"backd field exceeds {limit} bytes");
        return reader.ReadBytes(length).ToArray();
    }

    private sealed class Message
    {
        public uint Value0, Value1, Value2, Value3;
        public byte[] Bytes0 = [];
        public byte[] Bytes1 = [];
        public uint[] Array = [];
    }
}

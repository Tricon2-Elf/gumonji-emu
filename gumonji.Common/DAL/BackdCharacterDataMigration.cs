using gumonji.Common.Backd;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace gumonji.Common.DAL;

/// <summary>Completes the schema migration using the byte codec, atomically preserving old documents on failure.</summary>
public static class BackdCharacterDataMigration
{
    public static async Task ConvertAsync(MainContext db, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='backd.CharacterLegacy'";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct)) == 0)
        {
            await transaction.CommitAsync(ct);
            return;
        }
        command.CommandText = "SELECT UserId, Payload, UpdatedAt FROM \"backd.CharacterLegacy\" ORDER BY UserId";
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var uid = reader.GetInt64(0);
                try
                {
                    var character = BackdCharacterCodec.Decode(uid, (byte[])reader.GetValue(1));
                    _ = BackdCharacterCodec.Encode(character);
                    character.UpdatedAt = reader.GetDateTime(2);
                    db.BackdCharacters.Add(character);
                }
                catch (InvalidDataException ex)
                {
                    throw new InvalidDataException($"Cannot convert Backd character {uid}; legacy documents have been retained.", ex);
                }
            }
        }
        await db.SaveChangesAsync(ct);
        command.CommandText = "DROP TABLE \"backd.CharacterLegacy\"";
        await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }
}

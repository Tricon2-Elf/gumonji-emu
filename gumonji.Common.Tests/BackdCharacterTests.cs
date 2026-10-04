using System.Text;
using gumonji.Common.Backd;
using gumonji.Common.DAL;
using gumonji.Common.DAL.Entities;
using gumonji.Common.DAL.Repositories;
using gumonji.Network;
using gumonji.Network.Packets.Backd;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace gumonji.Common.Tests;

public sealed class BackdCharacterTests
{
    // Synthetic fixture follows the executable's writer order and exercises every recovered property.
    private static byte[] Fixture => Encoding.Latin1.GetBytes("""
        =character info file
        =owner_uid 4294967295
        =nickname AliceLEGACY_81@
        =mark_name badge
        =colortype skyblue
        =eyetype 2
        =subtype 3
        =favcolor 255
        =editlevel 99
        =novice 1
        =login_count 100
        =move_count -2147483648
        =consumption 9223372036854775807
        =clock_count 4294967295
        =myportal_server localhost
        =last_gumolot_time 123
        =last_login_time 4294967295
        =when_last_save_gumo_lltime -9223372036854775808
        =total_exchanged_money -123
        =total_exchanged_count 456
        =login_total_sec 789
        =create_unixtime 1000
        =iparam 0 -123
        =iparam 3 456
        # here follows item info
        =item 47 type ship
        =item 47 subtype 1
        =item 47 colortype red
        =item 47 iparam 0 -2147483648
        =item 47 iparam 7 2147483647
        =item 47 secret_iparam 7 -7
        =item 47 comment 0 EMPTY_COMMENT
        =item 47 comment 7 one two\n\r\Z\"\'\\LEGACY_82@
        =item 47 create_id unique-id
        =item 47 sticker_by_uidnum 4294967295
        =item 47 family_name two wordsLEGACY_83@
        =item 47 when_created_gumo_lltime 9223372036854775807
        =item 47 generation 4294967295
        =item 47 fert 4294967295
        =item 47 water 100
        =item 47 c6h4 200
        =item 47 sio2 300
        =item 47 caco3 400
        =item 47 price -1
        =equipflag 0 -1
        =equipflag 47 1
        # follows experience

        =ex 0 animal 1 2 3
        =ex 299 seed 255 254 253
        =future_field keep these bytesLEGACY_84@
        # end of character info file
        """.Replace("\r\n", "\n")
        .Replace("EMPTY_COMMENT", "")
        .Replace("LEGACY_81", "\u0081")
        .Replace("LEGACY_82", "\u0082")
        .Replace("LEGACY_83", "\u0083")
        .Replace("LEGACY_84", "\u0084"));

    [Fact]
    public void RecoveredWriterFixtureRoundTripsAndHasTypedFields()
    {
        var character = BackdCharacterCodec.Decode(42, Fixture);
        Assert.Equal(uint.MaxValue, character.OwnerUserId);
        Assert.Equal(Encoding.Latin1.GetBytes("Alice\u0081@"), character.Nickname);
        Assert.Equal(7, character.ColorType);
        Assert.Equal(int.MinValue, character.MoveCount);
        Assert.Equal(long.MaxValue, character.Consumption);
        Assert.Equal(long.MinValue, character.LastSaveGameTime);
        Assert.Equal((byte)255, character.FavoriteColor);
        var item = Assert.Single(character.Items);
        Assert.Equal((47, "ship", 1, 2), (item.Slot, item.TypeName, item.Subtype, item.ColorType));
        Assert.Equal(uint.MaxValue, item.Fertilizer);
        Assert.Equal(Encoding.Latin1.GetBytes("one two\n\r\u001a\"'\\\u0082@"), item.Comments.Single(x => x.Index == 7).Text);
        Assert.Equal(Encoding.Latin1.GetBytes("two words\u0083@"), item.FamilyName);
        Assert.Equal(255, character.Equipment.Single(x => x.Slot == 0).Value);
        Assert.Equal((4, 255, 254, 253),
            ((int)character.Experiences[1].Category, (int)character.Experiences[1].Type,
                (int)character.Experiences[1].Subtype, (int)character.Experiences[1].Color));
        Assert.Single(character.Extensions);
        Assert.Equal(Fixture, BackdCharacterCodec.Encode(character));
    }

    [Fact]
    public void MissingFieldsEmptyValuesDuplicatesAndEscapesRemainDistinct()
    {
        var character = BackdCharacterCodec.Decode(1, "=character info file\n=nickname \n=editlevel 1\n=editlevel 2\n=mark_name a\\\\b\n=iparam 0 1\n=iparam 0 2"u8);
        Assert.Empty(character.Nickname!);
        Assert.Null(character.LoginCount);
        Assert.Equal(2, character.EditLevel);
        Assert.Equal(2, Assert.Single(character.Parameters).Value);
        Assert.Equal("a\\b"u8.ToArray(), character.MarkName);
        var reloaded = BackdCharacterCodec.Decode(1, BackdCharacterCodec.Encode(character));
        Assert.Equal(character.MarkName, reloaded.MarkName);
        Assert.Null(reloaded.LoginCount);
    }

    [Theory]
    [InlineData("bad header")]
    [InlineData("=character info file\n=iparam 4 1")]
    [InlineData("=character info file\n=iparam -1 1")]
    [InlineData("=character info file\n=equipflag 48 1")]
    [InlineData("=character info file\n=ex 300 animal 1 2 3")]
    [InlineData("=character info file\n=ex 0 animal 256 2 3")]
    [InlineData("=character info file\n=ex 0 unknown 1 2 3")]
    [InlineData("=character info file\n=item 48 type ship")]
    [InlineData("=character info file\n=item 0 subtype 1")]
    [InlineData("=character info file\n=item 0 type ship\n=item 0 iparam 8 1")]
    [InlineData("=character info file\n=item 0 type ship\n=item 0 comment 8 danger")]
    [InlineData("=character info file\n=nickname bad\\q")]
    [InlineData("=character info file\n=nickname bad\\")]
    [InlineData("=character info file\n=owner_uid 4294967296")]
    [InlineData("=character info file\n=login_count nope")]
    [InlineData("=character info file\n=nickname embedded\0nul")]
    public void MalformedDocumentsAreRejected(string document) =>
        Assert.Throws<InvalidDataException>(() => BackdCharacterCodec.Decode(1, Encoding.Latin1.GetBytes(document)));

    [Fact]
    public void NamedColorsAndExperienceCategoriesUseOriginalCaseInsensitiveMatching()
    {
        var character = BackdCharacterCodec.Decode(1, "=character info file\n=colortype SkYbLuE\n=ex 0 SEED 1 2 3"u8);
        Assert.Equal(7, character.ColorType);
        Assert.Equal((byte)4, Assert.Single(character.Experiences).Category);
        Assert.Contains("=colortype skyblue", Encoding.ASCII.GetString(BackdCharacterCodec.Encode(character)));
    }

    [Fact]
    public void ByteAndPayloadLimitsAreEnforced()
    {
        var name = Enumerable.Repeat((byte)0x81, 127).ToArray();
        var character = new BackdCharacter { UserId = 1, Nickname = name };
        Assert.Equal(name, BackdCharacterCodec.Decode(1, BackdCharacterCodec.Encode(character)).Nickname);
        character.Nickname = new byte[128];
        Assert.Throws<InvalidDataException>(() => BackdCharacterCodec.Encode(character));
        Assert.Throws<InvalidDataException>(() => BackdCharacterCodec.Decode(1, []));
        Assert.Throws<InvalidDataException>(() => BackdCharacterCodec.Decode(1, new byte[262145]));
        character.Nickname = null;
        character.Extensions.Add(new() { Line = Enumerable.Repeat((byte)'#', 262144).ToArray() });
        Assert.Throws<InvalidDataException>(() => BackdCharacterCodec.Encode(character));
    }

    [Fact]
    public async Task StructuredRowsSurviveRestartAndReplacementRemovesOldChildren()
    {
        using var fixture = new Database();
        await fixture.MigrateAsync();
        var repository = new BackdCharacterRepository(fixture);
        await repository.SaveAsync(BackdCharacterCodec.Decode(42, Fixture));
        await using (var db = fixture.CreateDbContext())
        {
            Assert.Equal(2, await db.Set<BackdCharacterParameter>().CountAsync());
            Assert.Equal(3, await db.Set<BackdCharacterItemParameter>().CountAsync());
            Assert.Equal(2, await db.Set<BackdCharacterItemComment>().CountAsync());
            Assert.Equal(2, await db.Set<BackdCharacterExperience>().CountAsync());
            Assert.Equal(1, await db.Set<BackdCharacterExtension>().CountAsync());
            Assert.Equal(0, await Scalar(db, "SELECT count(*) FROM pragma_table_info('BackdCharacters') WHERE name='Payload'"));
        }
        repository = new BackdCharacterRepository(fixture); // fresh service/context, not an in-memory cache
        var restored = await repository.GetByUserIdAsync(42);
        Assert.Equal(Fixture, BackdCharacterCodec.Encode(restored!));
        restored!.Items[0].Price = 1234;
        await repository.SaveAsync(restored);
        Assert.Contains("=item 47 price 1234", Encoding.Latin1.GetString(BackdCharacterCodec.Encode((await repository.GetByUserIdAsync(42))!)));
        await repository.SaveAsync(new BackdCharacter { UserId = 42, EditLevel = 5 });
        await using var check = fixture.CreateDbContext();
        Assert.Empty(await check.Set<BackdCharacterItem>().ToListAsync());
        Assert.Empty(await check.Set<BackdCharacterItemComment>().ToListAsync());
        Assert.Empty(await check.Set<BackdCharacterExperience>().ToListAsync());
        Assert.Null(await repository.GetByUserIdAsync(999));
    }

    [Fact]
    public async Task LegacyDocumentsAreConvertedAndSourceIsRemovedOnlyAfterCommit()
    {
        using var fixture = new Database();
        var updated = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        await using (var db = fixture.CreateDbContext())
        {
            await db.GetService<IMigrator>().MigrateAsync("20260930092025_BackdHistories");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO BackdCharacters(UserId, Payload, UpdatedAt) VALUES (42, {Fixture}, {updated})");
            await db.Database.MigrateAsync();
            Assert.Equal(1, await Scalar(db, "SELECT count(*) FROM BackdCharacterLegacy"));
        }
        var repository = new BackdCharacterRepository(fixture);
        await repository.InitializeAsync();
        var restored = await repository.GetByUserIdAsync(42);
        Assert.Equal(Fixture, BackdCharacterCodec.Encode(restored!));
        Assert.Equal(updated, restored!.UpdatedAt);
        await repository.InitializeAsync();
        await new BackdCharacterRepository(fixture).InitializeAsync();
        await using var check = fixture.CreateDbContext();
        Assert.Equal(0, await Scalar(check, "SELECT count(*) FROM sqlite_master WHERE name='BackdCharacterLegacy'"));
        Assert.False(check.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task FailedLegacyConversionRetainsEverySourceAndWritesNoPartialCharacter()
    {
        using var fixture = new Database();
        await using (var db = fixture.CreateDbContext())
        {
            await db.GetService<IMigrator>().MigrateAsync("20260930092025_BackdHistories");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO BackdCharacters(UserId, Payload, UpdatedAt) VALUES (1, {Fixture}, {DateTime.UtcNow})");
            var invalid = "broken"u8.ToArray();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO BackdCharacters(UserId, Payload, UpdatedAt) VALUES (2, {invalid}, {DateTime.UtcNow})");
            await db.Database.MigrateAsync();
        }
        var repository = new BackdCharacterRepository(fixture);
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => repository.InitializeAsync());
        Assert.Contains("character 2", error.Message);
        await using var check = fixture.CreateDbContext();
        Assert.Equal(2, await Scalar(check, "SELECT count(*) FROM BackdCharacterLegacy"));
        Assert.Empty(await check.BackdCharacters.ToListAsync());
    }

    [Fact]
    public async Task CompetingSavesCommitOneWholeGraph()
    {
        using var fixture = new Database();
        await fixture.MigrateAsync();
        var first = new BackdCharacterRepository(fixture);
        var second = new BackdCharacterRepository(fixture);
        await first.InitializeAsync();
        await second.InitializeAsync();
        var a = BackdCharacterCodec.Decode(42, Fixture);
        var b = BackdCharacterCodec.Decode(42, Fixture);
        a.EditLevel = a.Items[0].Price = 11;
        b.EditLevel = b.Items[0].Price = 22;
        await Task.WhenAll(Task.Run(() => first.SaveAsync(a)), Task.Run(() => second.SaveAsync(b)));
        var saved = (await first.GetByUserIdAsync(42))!;
        Assert.Contains(saved.EditLevel, new int?[] { 11, 22 });
        Assert.Equal(saved.EditLevel, Assert.Single(saved.Items).Price);
        Assert.Equal(3, saved.Items[0].Parameters.Count);
    }

    [Fact]
    public async Task ReconstructedDocumentUsesTheOriginalLoadWireEnvelope()
    {
        using var fixture = new Database();
        await fixture.MigrateAsync();
        var dispatcher = TestPacketDispatcher.Create(fixture);
        (PacketType Id, byte[] Body) response = default;
        var session = new BackdSession((id, body, _) => { response = (id, body); return Task.CompletedTask; }) { ZoneName = "1" };
        var save = new PacketWriter();
        save.Write(5u);
        save.Write(42u);
        save.WriteCompactBytes(Fixture); // >252 bytes: exercise the extended compact length
        save.Write((uint)'I');
        await dispatcher.DispatchAsync(PacketType.BackdSaveCharacterRequest, save.ToBytes(), session);
        var saved = new PacketReader(response.Body);
        Assert.Equal(5u, saved.ReadUInt32());
        Assert.Equal(0u, saved.ReadUInt32());
        Assert.Equal((uint)'I', saved.ReadUInt32());
        saved.ExpectEnd();
        var load = new PacketWriter();
        load.Write(6u);
        load.Write(42u);
        load.WriteCompactCount(2);
        load.Write((uint)'L');
        load.Write(123u);
        await dispatcher.DispatchAsync(PacketType.BackdLoadCharacterRequest, load.ToBytes(), session);
        Assert.Equal(PacketType.BackdLoadCharacterReply, response.Id);
        var reader = new PacketReader(response.Body);
        Assert.Equal(6u, reader.ReadUInt32());
        Assert.Equal(42u, reader.ReadUInt32());
        Assert.Equal(0u, reader.ReadUInt32());
        Assert.Equal(Fixture, reader.ReadCompactBytes());
        Assert.Equal(2, reader.ReadCompactLength());
        Assert.Equal((uint)'L', reader.ReadUInt32());
        Assert.Equal(123u, reader.ReadUInt32());
        reader.ExpectEnd();
    }

    [Fact]
    public async Task FailedReplacementRollsBackTheDeletedGraph()
    {
        using var fixture = new Database();
        await fixture.MigrateAsync();
        var repository = new BackdCharacterRepository(fixture);
        await repository.SaveAsync(BackdCharacterCodec.Decode(42, Fixture));
        var invalid = BackdCharacterCodec.Decode(42, Fixture);
        invalid.Items[0].Comments.Add(new() { UserId = 42, Slot = 47, Index = 0, Text = "duplicate key"u8.ToArray() });
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SaveAsync(invalid));
        Assert.Equal(Fixture, BackdCharacterCodec.Encode((await repository.GetByUserIdAsync(42))!));
    }

    [Fact]
    public async Task InvalidSaveReturnsFailureAndDoesNotReplaceThePreviousCharacter()
    {
        using var fixture = new Database();
        await fixture.MigrateAsync();
        var state = new BackdState(fixture);
        await state.SaveCharacterAsync(42, Fixture, default);
        var dispatcher = TestPacketDispatcher.Create(fixture, state: state);
        (PacketType Id, byte[] Body) response = default;
        var session = new BackdSession((id, body, _) => { response = (id, body); return Task.CompletedTask; }) { ZoneName = "1" };
        var request = new PacketWriter();
        request.Write(99u);
        request.Write(42u);
        request.WriteCompactBytes("bad document"u8);
        request.Write((uint)'C');
        await dispatcher.DispatchAsync(PacketType.BackdSaveCharacterRequest, request.ToBytes(), session);
        Assert.Equal(PacketType.BackdSaveCharacterReply, response.Id);
        var reader = new PacketReader(response.Body);
        Assert.Equal(99u, reader.ReadUInt32());
        Assert.Equal(unchecked((uint)-6), reader.ReadUInt32());
        Assert.Equal((uint)'C', reader.ReadUInt32());
        reader.ExpectEnd();
        Assert.Equal(Fixture, BackdCharacterCodec.Encode((await state.GetCharacterAsync(42, default))!));
    }

    private static async Task<long> Scalar(MainContext db, string sql)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private sealed class Database : IDbContextFactory<MainContext>, IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), $"gumonji-character-{Guid.NewGuid():N}.db");
        public MainContext CreateDbContext() => new(new DbContextOptionsBuilder<MainContext>().UseSqlite($"Data Source={_path};Pooling=False").Options);
        public Task<MainContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
        public async Task MigrateAsync() { await using var db = CreateDbContext(); await db.Database.MigrateAsync(); }
        public void Dispose() { SqliteConnection.ClearAllPools(); File.Delete(_path); }
    }
}

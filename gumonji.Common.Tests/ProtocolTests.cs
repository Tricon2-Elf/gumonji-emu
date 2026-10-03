using System.Buffers.Binary;
using System.Numerics;
using gumonji.Common;
using gumonji.Network;
using gumonji.Network.Crypto;
using gumonji.Network.Packets.Femsg;
using gumonji.Network.Packets.Zone;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace gumonji.Common.Tests;

public class ProtocolTests
{
    [Fact]
    public void AesKnownAnswerAndLegacyKey()
    {
        var aes = new Aes128Ecb(Convert.FromHexString("000102030405060708090a0b0c0d0e0f"));
        var plain = Convert.FromHexString("00112233445566778899aabbccddeeff");
        var encrypted = Convert.FromHexString("69c4e0d86a7b0430d8cdb78070b4c55a");
        Assert.Equal(encrypted, aes.Encrypt(plain));
        Assert.Equal(plain, aes.Decrypt(encrypted));

        var shared = LegacyDh.ParseUnsignedHex("abcdef0123456789abcdef012345678912345678");
        Assert.Equal("12345601234567891234560123456789", Convert.ToHexString(LegacyDh.DeriveAesKey(shared)).ToLowerInvariant());
        Assert.Equal("0ABC", LegacyDh.BnHex(0xABC));
    }

    [Fact]
    public void GameEnvelopeRoundTrip()
    {
        var fixture = Convert.FromHexString("677a697008000000080000006c76360000000004000002da");
        var stream = Convert.FromHexString("00000004000002da");
        Assert.Equal(fixture, VceCompression.Wrap(stream));
        Assert.Equal(stream, VceCompression.Unwrap(fixture));
        Assert.Equal(Convert.FromHexString("000002da"), Assert.Single(new InnerFrames().Feed(stream)));

        var page = Frame(PacketType.PageDataResponse, new PageDataResponse(2, 2).ToBytes());
        var framed = Field(page);
        var frames = new InnerFrames();
        var reassembled = new List<byte[]>();
        for (var pos = 0; pos < framed.Length; pos += VceLimits.MaxRecordData)
        {
            var chunk = framed.AsSpan(pos, Math.Min(VceLimits.MaxRecordData, framed.Length - pos));
            reassembled.AddRange(frames.Feed(VceCompression.Unwrap(VceCompression.Wrap(chunk))));
        }
        Assert.Equal(page, Assert.Single(reassembled));
    }

    [Fact]
    public void SpawnAnimalUsesClientCowTemplate()
    {
        var reader = new PacketReader(new AnimalPlaceResponse(2000, 69, 66).ToBytes());
        Assert.Equal(2000u, reader.ReadUInt32());
        Assert.Equal((byte)3, reader.ReadByte()); // mammal, not reptile/snake
        Assert.Equal((byte)0, reader.ReadByte()); // cow_black
        Assert.Equal((byte)0, reader.ReadByte()); // black
    }

    [Fact]
    public void PlacedVehicleUsesToyCarBlackTemplate()
    {
        var reader = new PacketReader(new ItemPlaceResponse(3000, 62, 64).ToBytes());
        Assert.Equal(3000u, reader.ReadUInt32());
        Assert.Equal(ItemTemplateIds.ToyCar, reader.ReadUInt16());
        Assert.Equal((byte)0, reader.ReadByte()); // subtype
        Assert.Equal((byte)0, reader.ReadByte()); // black color
    }

    [Fact]
    public async Task AuthHandoffCharacterAndZoneMatchPython()
    {
        using var database = new TestDatabaseFixture();
        var options = new EmuOptions { ZonePort = 50000, AdvertiseIp = "127.0.0.1" };
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);
        var sent = new List<byte[]>();
        var front = Session(ServerKind.Femsg, database, options, sent);
        var auth = Assert.Single(await Receive(dispatcher, front, sent, "0065047573657206736563726574"));
        Assert.Equal("00660000000000000001", Convert.ToHexString(auth[..10]).ToLowerInvariant());
        var reader = new PacketReader(auth.AsSpan(10));
        Assert.Equal("Local Player"u8.ToArray(), reader.ReadCompactBytes());
        reader.ReadUInt32();
        reader.ReadUInt32();
        reader.ReadUInt32();
        Assert.Equal(100, reader.ReadCompactBytes().Length);
        Assert.Equal(1u, reader.ReadUInt32());
        Assert.Equal(0, reader.Remaining);

        var handoff = Assert.Single(await Receive(dispatcher, front, sent, "006901310100007f"));
        Assert.Equal("006a00000000", Convert.ToHexString(handoff[..6]).ToLowerInvariant());
        reader = new PacketReader(handoff.AsSpan(6));
        var token = reader.ReadCompactBytes();
        Assert.Equal(0, token[^1]);
        Assert.Equal("0100007fc350", Convert.ToHexString(handoff.AsSpan(handoff.Length - reader.Remaining)).ToLowerInvariant());

        var check = new PacketWriter();
        check.Write((uint)PacketType.CheckPasswordRequest);
        check.Write(1u);
        check.WriteCompactBytes(token[..^1]);
        var game = Session(ServerKind.Zone, database, options, sent);
        var accepted = Assert.Single(await Receive(dispatcher, game, sent, Convert.ToHexString(check.ToBytes())));
        Assert.Equal("000000dc0000000000010080008000000001", Convert.ToHexString(accepted).ToLowerInvariant());

        var menu = Assert.Single(await Receive(dispatcher, game, sent, "000002da"));
        Assert.Equal("000002db0000000000", Convert.ToHexString(menu).ToLowerInvariant());
        Assert.Equal(SessionState.CharacterCreationMenu, game.State);

        var ui = Assert.Single(await Receive(dispatcher, game, sent, "00002199"));
        Assert.Equal("0000219a", Convert.ToHexString(ui).ToLowerInvariant());
        var clock = Assert.Single(await Receive(dispatcher, game, sent, "00002328"));
        Assert.Equal("000023290000000000000c0001", Convert.ToHexString(clock).ToLowerInvariant());

        var created = Convert.FromHexString("000002e4000000000000000a00000004000000500c");
        var name = "Local Player"u8.ToArray();
        var createPacket = new byte[created.Length + name.Length];
        created.CopyTo(createPacket, 0);
        name.CopyTo(createPacket, created.Length);
        var createReplies = await Receive(dispatcher, game, sent, Convert.ToHexString(createPacket));
        Assert.Equal("000002e500000000", Convert.ToHexString(createReplies[0]).ToLowerInvariant());
        Assert.Equal("000002d00000000000000001", Convert.ToHexString(createReplies[1]).ToLowerInvariant());
        Assert.Equal(SessionState.CharacterCreated, game.State);
        Assert.Equal(1u, game.CharacterId);

        var boot = Assert.Single(await Receive(dispatcher, game, sent, "0000183a"));
        Assert.Equal("0000183b000000000000000000", Convert.ToHexString(boot).ToLowerInvariant());
        Assert.Empty(await Receive(dispatcher, game, sent, "000003b6"));
        Assert.False(game.SilentNoReply);
        var table = Assert.Single(await Receive(dispatcher, game, sent, "00002456"));
        Assert.Equal("000024570100010001000100", Convert.ToHexString(table).ToLowerInvariant());
        var names = Assert.Single(await Receive(dispatcher, game, sent, "000032ca"));
        Assert.Equal("000032cb00", Convert.ToHexString(names).ToLowerInvariant());
        var field = Assert.Single(await Receive(dispatcher, game, sent, "00003e8c00000000"));
        Assert.Equal("00003e8d00000000", Convert.ToHexString(field).ToLowerInvariant());

        var entered = await Receive(dispatcher, game, sent, "00000712012f0000000000");
        Assert.Equal("0000071c0000000000400040000131", Convert.ToHexString(entered[0]).ToLowerInvariant());
        Assert.Equal(
            "000005280000000100000000000001060000fa000000fa00000000000000000000000000000000000000000000200000000000000000",
            Convert.ToHexString(entered[1]).ToLowerInvariant());
        Assert.Equal(1, entered[1][14]);
        Assert.Equal((64000u, 64000u), (
            BinaryPrimitives.ReadUInt32BigEndian(entered[1].AsSpan(16)),
            BinaryPrimitives.ReadUInt32BigEndian(entered[1].AsSpan(20))));
        var avatar = Convert.FromHexString(
            "000005f0000000000000000101000a040c4c6f63616c20506c61796572"
            + "0000000000000000000000000000005000000000000000000000000000");
        Assert.Equal(avatar, entered[2]);
        Assert.Equal(SessionState.ZoneEntered, game.State);

        Assert.Empty(await Receive(dispatcher, game, sent, "0000203a0000000300000001"));
        Assert.True(game.SilentNoReply);

        var page = await Receive(dispatcher, game, sent, "000001ea0000000200000002");
        Assert.Equal(23603, page[0].Length);
        Assert.Equal("000001f4000000020000000200000000", Convert.ToHexString(page[0][..16]).ToLowerInvariant());
        Assert.True(page.Count - 1 >= 8);
        Assert.Equal(
            "00001fa5000003e80301080000008ca000008ca00000000100000000040000460045000000000000",
            Convert.ToHexString(page[1]).ToLowerInvariant());
        Assert.Contains(page, p => BinaryPrimitives.ReadUInt32BigEndian(p) == (uint)PacketType.AnimalPlaceResponse);
        Assert.Contains(page, p => BinaryPrimitives.ReadUInt32BigEndian(p) == (uint)PacketType.ItemPlaceResponse);
        var again = await Receive(dispatcher, game, sent, "000001ea0000000200000002");
        Assert.Single(again);

        Assert.Empty(await Receive(dispatcher, game, sent, "0000052b0000fa000000fa00"));
        Assert.True(game.SilentNoReply);
        Assert.Empty(await Receive(dispatcher, game, sent, "0000051e0000000100ffffffff0102000107b40000fa0000000468200000000000000000"));
        Assert.True(game.SilentNoReply);

        Assert.Empty(await Receive(dispatcher, game, sent, "000006220000001302"));
        Assert.True(game.SilentNoReply);
        Assert.Equal(new ActionEmoteState(0x13, 2), game.LastActionEmote);

        Assert.Empty(await Receive(dispatcher, game, sent, "0000063000000012"));
        Assert.True(game.SilentNoReply);
        Assert.Equal(0x12u, game.LastFacialEmoteId);

        var chat = Assert.Single(await Receive(dispatcher, game, sent, "0000045604686f67650568656c6c6f"));
        Assert.Equal((uint)PacketType.ChatEventResponse, BinaryPrimitives.ReadUInt32BigEndian(chat));
        Assert.Equal(
            Frame(PacketType.ChatEventResponse, new ChatEventResponse(1, "Local Player"u8.ToArray(), "hello"u8.ToArray()).ToBytes()),
            chat);
        Assert.Equal(
            "0000046000000000000000010000000000000c4c6f63616c20506c61796572000568656c6c6f",
            Convert.ToHexString(chat).ToLowerInvariant());
        Assert.Equal("hoge"u8.ToArray(), game.LastChat!.Sender);
        Assert.Equal("hello"u8.ToArray(), game.LastChat.Message);

        Assert.Empty(await Receive(dispatcher, game, sent, "0000046a"));
        Assert.True(game.SilentNoReply);
        Assert.True(game.IsTypingInChat);

        var notice = Session(ServerKind.Femsg, database, options, sent);
        notice.UserId = 1;
        notice.State = SessionState.HandoffIssued;
        var members = Assert.Single(await Receive(dispatcher, notice, sent, "0198"));
        Assert.Equal("0199000000000000", Convert.ToHexString(members).ToLowerInvariant());
        Assert.False(notice.SilentNoReply);
        Assert.Empty(await Receive(dispatcher, notice, sent, "01a500000002"));
        Assert.True(notice.SilentNoReply);
        Assert.Empty(await Receive(dispatcher, notice, sent, "01a400000012"));
        Assert.True(notice.SilentNoReply);
        Assert.Equal(0x12u, notice.LastFrontendAnimationId);
        Assert.Empty(await Receive(dispatcher, notice, sent, "01a400000000"));
        Assert.Equal(0u, notice.LastFrontendAnimationId);
        Assert.Throws<InvalidDataException>(() => AvatarAnimationNotice.FromBytes([0, 0, 0]));

        var character = await database.Characters.GetByUserIdAsync(1);
        Assert.NotNull(character);
        Assert.Equal("Local Player"u8.ToArray(), character!.Name);
        Assert.Equal((0, 10, 4, 0x50), (character.Body, character.Model, character.Style, character.Color));

        var profile = Session(ServerKind.Femsg, database, options, sent);
        profile.UserId = 1;
        profile.State = SessionState.WaitZoneRequest;
        Assert.Empty(await Receive(dispatcher, profile, sent, "08fd00000001"));
        Assert.True(profile.SilentNoReply);

        var returning = Session(ServerKind.Zone, database, options, sent);
        returning.UserId = 1;
        returning.State = SessionState.WaitCharacterCheck;
        var existing = Assert.Single(await Receive(dispatcher, returning, sent, "000002da"));
        Assert.Equal("000002db0000000100", Convert.ToHexString(existing).ToLowerInvariant());
        Assert.Equal(SessionState.WaitCharacterLoad, returning.State);
        var assigned = Assert.Single(await Receive(dispatcher, returning, sent, "000002c6"));
        Assert.Equal("000002d00000000000000001", Convert.ToHexString(assigned).ToLowerInvariant());
        Assert.Equal(SessionState.CharacterCreated, returning.State);
        Assert.Equal(1u, returning.CharacterId);

        await Assert.ThrowsAsync<InvalidDataException>(() => Receive(dispatcher, game, sent, "0000232800"));
        var replay = Session(ServerKind.Zone, database, options, sent);
        await Assert.ThrowsAsync<InvalidDataException>(() => Receive(dispatcher, replay, sent, Convert.ToHexString(check.ToBytes())));
    }

    [Fact]
    public async Task FrontendInventoryTemplateSyncAfterAvatarUpdateIsOneWay()
    {
        using var database = new TestDatabaseFixture();
        var sent = new List<byte[]>();
        var frontend = Session(ServerKind.Femsg, database, new EmuOptions(), sent);
        frontend.UserId = 1;
        frontend.State = SessionState.HandoffIssued;
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);

        Assert.Empty(await Receive(dispatcher, frontend, sent, "01a301005001000100"));
        Assert.True(frontend.SilentNoReply);
        Assert.Equal(new ushort[] { ItemTemplateIds.ToyCar }, frontend.LastInventoryTemplates!.ItemTypes);
        Assert.Equal(new byte[] { 0 }, frontend.LastInventoryTemplates.Subtypes);
        Assert.Equal(new byte[] { 0 }, frontend.LastInventoryTemplates.Colors);
        Assert.Throws<InvalidDataException>(() => InventoryTemplateNotice.FromBytes([1, 2, 0x3A, 0, 0]));
    }

    [Fact]
    public async Task FrontendProgressValueGetsFourByteReply()
    {
        var sent = new List<byte[]>();
        using var database = new TestDatabaseFixture();
        var frontend = Session(ServerKind.Femsg, database, new EmuOptions(), sent);
        frontend.State = SessionState.HandoffIssued;
        var dispatcher = PacketDispatcher.CreateDefault(NullLogger<PacketDispatcher>.Instance);
        var response = Assert.Single(await Receive(dispatcher, frontend, sent, "013200000064"));
        Assert.Equal("013300000000", Convert.ToHexString(response).ToLowerInvariant());
        Assert.Throws<InvalidDataException>(() => ProgressValueRequest.FromBytes([0, 0, 0]));
    }

    private static GumonjiSession Session(ServerKind kind, TestDatabaseFixture database, EmuOptions options, List<byte[]> sent) =>
        new(kind, database.Accounts, database.Characters, database.LoginTokens, database.Gameplay,
            options, (type, body, _) =>
        {
            sent.Add(Frame(type, body));
            return Task.CompletedTask;
        });

    private static async Task<List<byte[]>> Receive(PacketDispatcher dispatcher, GumonjiSession session, List<byte[]> sent, string hex)
    {
        var packet = Convert.FromHexString(hex);
        var width = session.Kind == ServerKind.Zone ? 4 : 2;
        var opcode = width == 2
            ? BinaryPrimitives.ReadUInt16BigEndian(packet)
            : BinaryPrimitives.ReadUInt32BigEndian(packet);
        var before = sent.Count;
        session.SilentNoReply = false;
        await dispatcher.DispatchAsync(session.Kind, (PacketType)opcode, packet.AsMemory(width), session);
        return sent.Skip(before).ToList();
    }

    private static byte[] Frame(PacketType type, byte[] body)
    {
        var width = (uint)type <= 0xFFFF && PacketTypeInfo.OpcodeWidth(ServerKind.Femsg) == 2 && IsFemsg(type) ? 2 : 4;
        var packet = new byte[width + body.Length];
        if (width == 2)
            BinaryPrimitives.WriteUInt16BigEndian(packet, (ushort)type);
        else
            BinaryPrimitives.WriteUInt32BigEndian(packet, (uint)type);
        body.CopyTo(packet, width);
        return packet;
    }

    private static bool IsFemsg(PacketType type) => type is
        PacketType.HeartbeatRequest or PacketType.HeartbeatReply or PacketType.LoginRequest or PacketType.LoginAcceptResponse
        or PacketType.ZoneConnectRequest or PacketType.ZoneHandoffResponse or PacketType.HomeZoneRequest or PacketType.HomeZoneReply
        or PacketType.ZoneEnteredNotice or PacketType.ZoneMemberListResponse or PacketType.InventoryTemplateNotice
        or PacketType.AvatarAnimationNotice
        or PacketType.ProgressValueRequest or PacketType.ProgressValueResponse
        or PacketType.PlayerStateNotice or PacketType.ProfileRequest;

    private static byte[] Field(byte[] payload)
    {
        var framed = new byte[4 + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(framed, payload.Length);
        payload.CopyTo(framed, 4);
        return framed;
    }
}

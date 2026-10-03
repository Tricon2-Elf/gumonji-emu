using System.Reflection;

namespace gumonji.Network;

public enum ServerKind
{
    Femsg,
    Zone,
    Backd,
}

public enum PacketDirection
{
    ClientToServer,
    ServerToClient,
}

public enum ImplementationState
{
    NotImplemented,
    Implemented,
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class PacketMetadata(
    ServerKind server,
    PacketDirection direction,
    string decompiledName,
    ImplementationState state = ImplementationState.Implemented
) : Attribute
{
    public ServerKind Server { get; } = server;
    public PacketDirection Direction { get; } = direction;
    public string DecompiledName { get; } = decompiledName;
    public ImplementationState State { get; } = state;
}

/// <summary>
/// Wire opcode. Frontend/backd packets are written as 16 bits and zone packets as 32 bits;
/// the numeric value is the same either way.
/// </summary>
public enum PacketType : uint
{
    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "LOGOUT")]
    LogoutRequest = 0x0096,
    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "GET_RENDER_TASK")]
    RenderTaskRequest = 0x0582,
    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "RENDER_TASK")]
    RenderTaskResponse = 0x0583,
    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "INFO_POSITION")]
    EntityPositionRequest = 0x05F3,
    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "INFO_POSITION_RESPONSE")]
    EntityPositionResponse = 0x05F4,
    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "TAKEOFF")]
    TakeoffRequest = 0x077A,
    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "REPORT_ENVIRONMENT")]
    EnvironmentReportRequest = 0x0BC2,
    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "NEAREST_CHARACTER")]
    NearestCharacterRequest = 0x3EE4,
    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "NEAREST_CHARACTER_RESPONSE")]
    NearestCharacterResponse = 0x3EE5,

    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "LoginRequest")]
    BackdLoginRequest = 1,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "LoginReply")]
    BackdLoginReply = 2,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "StatusRequest")]
    BackdStatusRequest = 5,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "StatusReply")]
    BackdStatusReply = 6,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "CheckPasswordRequest")]
    BackdCheckPasswordRequest = 107,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "CheckPasswordReply")]
    BackdCheckPasswordReply = 108,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "GetLockRequest")]
    BackdGetLockRequest = 201,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "GetLockReply")]
    BackdGetLockReply = 202,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "PutLockRequest")]
    BackdPutLockRequest = 203,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "PutLockReply")]
    BackdPutLockReply = 204,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "SaveCharacterRequest")]
    BackdSaveCharacterRequest = 501,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "SaveCharacterReply")]
    BackdSaveCharacterReply = 502,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "LoadCharacterRequest")]
    BackdLoadCharacterRequest = 503,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "LoadCharacterReply")]
    BackdLoadCharacterReply = 504,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "CharacterExistsRequest")]
    BackdCharacterExistsRequest = 507,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "CharacterExistsReply")]
    BackdCharacterExistsReply = 508,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "UserOnlineRequest")]
    BackdUserOnlineRequest = 1301,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "UserOfflineRequest")]
    BackdUserOfflineRequest = 1302,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "AllocateDoorIdsRequest")]
    BackdAllocateDoorIdsRequest = 1501,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "AllocateDoorIdsReply")]
    BackdAllocateDoorIdsReply = 1502,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "SellerIdsRequest")]
    BackdSellerIdsRequest = 1701,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "VendorZonesRequest")]
    BackdVendorZonesRequest = 1702,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "VendorZonesReply")]
    BackdVendorZonesReply = 1703,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "AuditRequest")]
    BackdAuditRequest = 2001,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "SaveHistoryRequest")]
    BackdSaveHistoryRequest = 2101,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "SaveHistoryReply")]
    BackdSaveHistoryReply = 2102,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "LoadHistoryRequest")]
    BackdLoadHistoryRequest = 2111,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "LoadHistoryReply")]
    BackdLoadHistoryReply = 2112,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ClientToServer, "PassageLinksRequest")]
    BackdPassageLinksRequest = 2409,
    [PacketMetadata(ServerKind.Backd, PacketDirection.ServerToClient, "PassageLinksReply")]
    BackdPassageLinksReply = 2410,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "MOVEMENT_TOTALS")]
    MovementTotalsRequest = 0x3E80,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "LOOK_MYCHAR")]
    CharacterConditionRequest = 0x0866,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "MYCHAR_CONDITION")]
    CharacterConditionResponse = 0x0868,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "PLANT_HARVEST")]
    PlantHarvestRequest = 0x1FB8,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "INVENTORY_SLOT")]
    InventorySlotResponse = 0x03C0,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "HEARTBEAT")]
    HeartbeatRequest = 0x0005,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ServerToClient, "HEARTBEAT_REPLY")]
    HeartbeatReply = 0x0006,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "LOGIN")]
    LoginRequest = 0x0065,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ServerToClient, "LOGIN_ACCEPT")]
    LoginAcceptResponse = 0x0066,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "ZONE_CONNECT_REQUEST")]
    ZoneConnectRequest = 0x0069,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ServerToClient, "ZONE_HANDOFF")]
    ZoneHandoffResponse = 0x006A,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "HOME_ZONE_REQUEST")]
    HomeZoneRequest = 0x006E,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ServerToClient, "HOME_ZONE_REPLY")]
    HomeZoneReply = 0x006F,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "ZONE_ENTERED_NOTICE")]
    ZoneEnteredNotice = 0x0198,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ServerToClient, "ZONE_MEMBER_LIST")]
    ZoneMemberListResponse = 0x0199,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "PROGRESS_VALUE")]
    ProgressValueRequest = 0x0132,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ServerToClient, "PROGRESS_VALUE_REPLY")]
    ProgressValueResponse = 0x0133,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "INVENTORY_TEMPLATE_SYNC")]
    InventoryTemplateNotice = 0x01A3,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "AVATAR_ANIMATION")]
    AvatarAnimationNotice = 0x01A4,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "PLAYER_STATE")]
    PlayerStateNotice = 0x01A5,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "PROFILE_REQUEST")]
    ProfileRequest = 0x08FD,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "TUTORIAL_COMPLETE")]
    TutorialCompleteRequest = 0x0899,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ServerToClient, "TUTORIAL_COMPLETE_RESULT")]
    TutorialCompleteResponse = 0x089A,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "PING")]
    PingRequest = 0x0001,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "PONG")]
    PingResponse = 0x0002,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHECK_PASSWORD")]
    CheckPasswordRequest = 0x00D2,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "CHECK_PASSWORD_ACCEPT")]
    CheckPasswordAcceptResponse = 0x00DC,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "GET_PAGE_DATA")]
    GetPageDataRequest = 0x01EA,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "PAGE_DATA")]
    PageDataResponse = 0x01F4,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHARACTER_LOAD")]
    CharacterLoadRequest = 0x02C6,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "CHARACTER_ASSIGN")]
    CharacterAssignResponse = 0x02D0,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHARACTER_CHECK_EXIST")]
    CharacterCheckExistRequest = 0x02DA,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "CHARACTER_CHECK_EXIST_RESPONSE")]
    CharacterCheckExistResponse = 0x02DB,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHARACTER_CREATE")]
    CharacterCreateRequest = 0x02E4,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "CHARACTER_CREATE_ACCEPT")]
    CharacterCreateAcceptResponse = 0x02E5,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "ZONE_ANNOUNCE")]
    ZoneAnnounceRequest = 0x03B6,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "CHAT_EVENT")]
    ChatEventResponse = 0x0460,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "MOVEMENT")]
    MovementRequest = 0x051E,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "ENTITY_PLACE")]
    EntityPlaceResponse = 0x0528,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "POSITION_REPORT")]
    PositionReportRequest = 0x052B,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHAT")]
    ChatRequest = 0x0456,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHAT_TYPING")]
    ChatTypingRequest = 0x046A,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "ACTION_EMOTE")]
    ActionEmoteRequest = 0x0622,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "FACIAL_EMOTE")]
    FacialEmoteRequest = 0x0630,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "CHARACTER_AVATAR")]
    CharacterAvatarResponse = 0x05F0,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "ITEM_USE")]
    ItemUseRequest = 0x077C,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "ITEM_USE_RESULT")]
    ItemUseResponse = 0x077D,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "ITEM_PICKUP")]
    ItemPickupRequest = 0x0776,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "ITEM_PICKUP_RESULT")]
    ItemPickupResponse = 0x0777,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "ZONE_ENTER_REQUEST")]
    ZoneEnterRequest = 0x0712,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "ZONE_ENTER")]
    ZoneEnterResponse = 0x071C,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHUNK_LOADED")]
    ChunkLoadedRequest = 0x08EF,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "PERIODIC_REPORT")]
    PeriodicReportRequest = 0x0BB8,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHUNK_SUBSCRIBE")]
    ChunkSubscribe0Bcc = 0x0BCC,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "ZONE_BOOT_REQUEST")]
    ZoneBootRequest = 0x183A,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "ZONE_BOOT_ACK")]
    ZoneBootAckResponse = 0x183B,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHUNK_SUBSCRIBE")]
    ChunkSubscribe1Fa4 = 0x1FA4,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "PLANT_PLACE")]
    PlantPlaceResponse = 0x1FA5,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "ANIMAL_PLACE")]
    AnimalPlaceResponse = 0x2009,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "ANIMAL_MOVE_REQUEST")]
    AnimalMoveRequest = 0x200A,

    // Original zonesv decodes these requests and discards their fields.
    // Keep neutral names until their intended protocol semantics are recovered.
    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "UNNAMED_1B62")]
    NoOp1B62Request = 0x1B62,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "UNNAMED_1FC2")]
    NoOp1FC2Request = 0x1FC2,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "UNNAMED_2082")]
    NoOp2082Request = 0x2082,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHUNK_SUBSCRIBE")]
    ChunkSubscribe200C = 0x200C,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHUNK_SUBSCRIBE")]
    ChunkSubscribe203A = 0x203A,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHUNK_SUBSCRIBE")]
    ChunkSubscribe206C = 0x206C,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "ITEM_PLACE")]
    ItemPlaceResponse = 0x206D,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "ITEM_REMOVE")]
    ItemRemoveResponse = 0x206F,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHARACTER_UI_OPEN")]
    CharacterUiOpenRequest = 0x2199,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "CHARACTER_UI_ACK")]
    CharacterUiAckResponse = 0x219A,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CLOCK_REQUEST")]
    ClockRequest = 0x2328,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "CLOCK_SYNC")]
    ClockSyncResponse = 0x2329,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "BYTE_TABLE_REQUEST")]
    ByteTableRequest = 0x2456,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "BYTE_TABLE")]
    ByteTableResponse = 0x2457,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "STRING_LIST_REQUEST")]
    StringListRequest = 0x32CA,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "STRING_LIST")]
    StringListResponse = 0x32CB,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ClientToServer, "CHARACTER_FIELD_REQUEST")]
    CharacterFieldRequest = 0x3E8C,

    [PacketMetadata(ServerKind.Zone, PacketDirection.ServerToClient, "CHARACTER_FIELD_ACK")]
    CharacterFieldAckResponse = 0x3E8D,
}

public static class PacketTypeInfo
{
    private static readonly Dictionary<(ServerKind Server, uint Opcode), string> Names =
        typeof(PacketType).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (Field: field, Metadata: field.GetCustomAttribute<PacketMetadata>()))
            .Where(entry => entry.Metadata is not null)
            .ToDictionary(entry => (entry.Metadata!.Server, (uint)(PacketType)entry.Field.GetValue(null)!),
                entry => entry.Field.Name);

    public static int OpcodeWidth(ServerKind kind) => kind == ServerKind.Zone ? 4 : 2;

    public static string Name(ServerKind kind, PacketType type) =>
        Names.TryGetValue((kind, (uint)type), out var name) ? name : $"0x{(uint)type:X}";

    public static bool IsDefined(uint opcode) => Enum.IsDefined(typeof(PacketType), opcode);
}

namespace gumonji.Network;

public enum ServerKind
{
    Femsg,
    Game,
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
/// Wire opcode. Frontend packets are written as 16 bits and game packets as 32 bits;
/// the numeric value is the same either way.
/// </summary>
public enum PacketType : uint
{
    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "MOVEMENT_TOTALS")]
    MovementTotalsRequest = 0x3E80,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "LOOK_MYCHAR")]
    CharacterConditionRequest = 0x0866,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "MYCHAR_CONDITION")]
    CharacterConditionResponse = 0x0868,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "PLANT_HARVEST")]
    PlantHarvestRequest = 0x1FB8,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "INVENTORY_SLOT")]
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

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "INVENTORY_TEMPLATE_SYNC")]
    InventoryTemplateNotice = 0x01A3,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "PLAYER_STATE")]
    PlayerStateNotice = 0x01A5,

    [PacketMetadata(ServerKind.Femsg, PacketDirection.ClientToServer, "PROFILE_REQUEST")]
    ProfileRequest = 0x08FD,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "PING")]
    PingRequest = 0x0001,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "PONG")]
    PingResponse = 0x0002,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHECK_PASSWORD")]
    CheckPasswordRequest = 0x00D2,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "CHECK_PASSWORD_ACCEPT")]
    CheckPasswordAcceptResponse = 0x00DC,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "GET_PAGE_DATA")]
    GetPageDataRequest = 0x01EA,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "PAGE_DATA")]
    PageDataResponse = 0x01F4,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHARACTER_LOAD")]
    CharacterLoadRequest = 0x02C6,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "CHARACTER_ASSIGN")]
    CharacterAssignResponse = 0x02D0,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHARACTER_CHECK_EXIST")]
    CharacterCheckExistRequest = 0x02DA,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "CHARACTER_CHECK_EXIST_RESPONSE")]
    CharacterCheckExistResponse = 0x02DB,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHARACTER_CREATE")]
    CharacterCreateRequest = 0x02E4,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "CHARACTER_CREATE_ACCEPT")]
    CharacterCreateAcceptResponse = 0x02E5,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "ZONE_ANNOUNCE")]
    ZoneAnnounceRequest = 0x03B6,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "CHAT_EVENT")]
    ChatEventResponse = 0x0460,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "MOVEMENT")]
    MovementRequest = 0x051E,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "ENTITY_PLACE")]
    EntityPlaceResponse = 0x0528,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "POSITION_REPORT")]
    PositionReportRequest = 0x052B,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHAT")]
    ChatRequest = 0x0456,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHAT_TYPING")]
    ChatTypingRequest = 0x046A,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "ACTION_EMOTE")]
    ActionEmoteRequest = 0x0622,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "FACIAL_EMOTE")]
    FacialEmoteRequest = 0x0630,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "CHARACTER_AVATAR")]
    CharacterAvatarResponse = 0x05F0,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "ITEM_USE")]
    ItemUseRequest = 0x077C,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "ITEM_USE_RESULT")]
    ItemUseResponse = 0x077D,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "ITEM_PICKUP")]
    ItemPickupRequest = 0x0776,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "ITEM_PICKUP_RESULT")]
    ItemPickupResponse = 0x0777,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "ZONE_ENTER_REQUEST")]
    ZoneEnterRequest = 0x0712,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "ZONE_ENTER")]
    ZoneEnterResponse = 0x071C,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHUNK_LOADED")]
    ChunkLoadedRequest = 0x08EF,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "PERIODIC_REPORT")]
    PeriodicReportRequest = 0x0BB8,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHUNK_SUBSCRIBE")]
    ChunkSubscribe0Bcc = 0x0BCC,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "ZONE_BOOT_REQUEST")]
    ZoneBootRequest = 0x183A,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "ZONE_BOOT_ACK")]
    ZoneBootAckResponse = 0x183B,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHUNK_SUBSCRIBE")]
    ChunkSubscribe1Fa4 = 0x1FA4,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "PLANT_PLACE")]
    PlantPlaceResponse = 0x1FA5,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "ANIMAL_PLACE")]
    AnimalPlaceResponse = 0x2009,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "ANIMAL_MOVE_REQUEST")]
    AnimalMoveRequest = 0x200A,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHUNK_SUBSCRIBE")]
    ChunkSubscribe200C = 0x200C,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHUNK_SUBSCRIBE")]
    ChunkSubscribe203A = 0x203A,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHUNK_SUBSCRIBE")]
    ChunkSubscribe206C = 0x206C,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "ITEM_PLACE")]
    ItemPlaceResponse = 0x206D,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "ITEM_REMOVE")]
    ItemRemoveResponse = 0x206F,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHARACTER_UI_OPEN")]
    CharacterUiOpenRequest = 0x2199,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "CHARACTER_UI_ACK")]
    CharacterUiAckResponse = 0x219A,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CLOCK_REQUEST")]
    ClockRequest = 0x2328,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "CLOCK_SYNC")]
    ClockSyncResponse = 0x2329,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "BYTE_TABLE_REQUEST")]
    ByteTableRequest = 0x2456,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "BYTE_TABLE")]
    ByteTableResponse = 0x2457,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "STRING_LIST_REQUEST")]
    StringListRequest = 0x32CA,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "STRING_LIST")]
    StringListResponse = 0x32CB,

    [PacketMetadata(ServerKind.Game, PacketDirection.ClientToServer, "CHARACTER_FIELD_REQUEST")]
    CharacterFieldRequest = 0x3E8C,

    [PacketMetadata(ServerKind.Game, PacketDirection.ServerToClient, "CHARACTER_FIELD_ACK")]
    CharacterFieldAckResponse = 0x3E8D,
}

public static class PacketTypeInfo
{
    public static int OpcodeWidth(ServerKind kind) => kind == ServerKind.Game ? 4 : 2;

    public static bool IsDefined(uint opcode) => Enum.IsDefined(typeof(PacketType), opcode);
}

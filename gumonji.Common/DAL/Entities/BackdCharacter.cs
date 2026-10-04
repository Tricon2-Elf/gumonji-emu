namespace gumonji.Common.DAL.Entities;

/// <summary>Persistent character fields for the original Zone/Backd contract.</summary>
public sealed class BackdCharacter
{
    public long UserId { get; set; }
    public uint? OwnerUserId { get; set; }
    // Individual legacy text fields remain bytes; there is no serialized document column.
    public byte[]? Nickname { get; set; }
    public byte[]? MarkName { get; set; }
    public int? ColorType { get; set; }
    public int? EyeType { get; set; }
    public int? Subtype { get; set; }
    public byte? FavoriteColor { get; set; }
    public int? EditLevel { get; set; }
    public int? Novice { get; set; }
    public int? LoginCount { get; set; }
    public int? MoveCount { get; set; }
    public long? Consumption { get; set; }
    public uint? ClockCount { get; set; }
    public byte[]? PortalServer { get; set; }
    public uint? LastGumolotTime { get; set; }
    public uint? LastLoginTime { get; set; }
    public long? LastSaveGameTime { get; set; }
    public long? TotalExchangedMoney { get; set; }
    public long? TotalExchangedCount { get; set; }
    public uint? LoginTotalSeconds { get; set; }
    public uint? CreatedUnixTime { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<BackdCharacterParameter> Parameters { get; set; } = [];
    public List<BackdCharacterItem> Items { get; set; } = [];
    public List<BackdCharacterEquipment> Equipment { get; set; } = [];
    public List<BackdCharacterExperience> Experiences { get; set; } = [];
    public List<BackdCharacterExtension> Extensions { get; set; } = [];
}

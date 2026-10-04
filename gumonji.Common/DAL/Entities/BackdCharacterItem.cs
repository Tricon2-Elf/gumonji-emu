namespace gumonji.Common.DAL.Entities;

public sealed class BackdCharacterItem
{
    public long UserId { get; set; }
    public int Slot { get; set; }
    public string TypeName { get; set; } = "";
    public int? Subtype { get; set; }
    public int? ColorType { get; set; }
    public byte[]? CreateId { get; set; }
    public uint? StickerUserId { get; set; }
    public byte[]? FamilyName { get; set; }
    public long? CreatedGameTime { get; set; }
    public uint? Generation { get; set; }
    public uint? Fertilizer { get; set; }
    public uint? Water { get; set; }
    public uint? C6H4 { get; set; }
    public uint? SiO2 { get; set; }
    public uint? CaCO3 { get; set; }
    public int? Price { get; set; }
    public List<BackdCharacterItemParameter> Parameters { get; set; } = [];
    // Fixed eight-field layout in the original character document. Null means omitted.
    public string? Comment0 { get; set; }
    public string? Comment1 { get; set; }
    public string? Comment2 { get; set; }
    public string? Comment3 { get; set; }
    public string? Comment4 { get; set; }
    public string? Comment5 { get; set; }
    public string? Comment6 { get; set; }
    public string? Comment7 { get; set; }
}

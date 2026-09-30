namespace gumonji.Common.DAL.Entities;

/// <summary>Persistent allocator for IDs issued to the original zone server.</summary>
public sealed class BackdSequence
{
    public string Name { get; set; } = "";
    public long NextId { get; set; } = 1;
}

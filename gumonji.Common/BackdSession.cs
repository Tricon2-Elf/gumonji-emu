namespace gumonji.Common;

public sealed class BackdSession
{
    public Guid Id { get; } = Guid.NewGuid();
    public string? ZoneName { get; set; }
    public bool Authenticated => ZoneName is not null;
}

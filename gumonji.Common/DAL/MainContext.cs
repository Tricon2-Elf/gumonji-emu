using gumonji.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace gumonji.Common.DAL;

public sealed class MainContext(DbContextOptions<MainContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<LoginToken> LoginTokens => Set<LoginToken>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<TutorialCompletion> TutorialCompletions => Set<TutorialCompletion>();
    public DbSet<BackdCharacter> BackdCharacters => Set<BackdCharacter>();
    public DbSet<BackdSequence> BackdSequences => Set<BackdSequence>();
    public DbSet<BackdHistory> BackdHistories => Set<BackdHistory>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<BackdCharacter>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).ValueGeneratedNever();
            e.HasMany(x => x.Parameters).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Equipment).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Experiences).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Extensions).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<BackdCharacterParameter>().HasKey(x => new { x.UserId, x.Index });
        b.Entity<BackdCharacterEquipment>().HasKey(x => new { x.UserId, x.Slot });
        b.Entity<BackdCharacterExperience>().HasKey(x => new { x.UserId, x.Index });
        b.Entity<BackdCharacterExtension>().HasKey(x => new { x.UserId, x.Index });
        b.Entity<BackdCharacterItem>(e =>
        {
            e.HasKey(x => new { x.UserId, x.Slot });
            e.HasMany(x => x.Parameters).WithOne().HasForeignKey(x => new { x.UserId, x.Slot }).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Comments).WithOne().HasForeignKey(x => new { x.UserId, x.Slot }).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<BackdCharacterItemParameter>().HasKey(x => new { x.UserId, x.Slot, x.Secret, x.Index });
        b.Entity<BackdCharacterItemComment>().HasKey(x => new { x.UserId, x.Slot, x.Index });
        b.Entity<BackdSequence>(e =>
        {
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).IsRequired();
        });
        b.Entity<BackdHistory>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).ValueGeneratedNever();
            e.Property(x => x.Payload).IsRequired();
        });
        b.Entity<InventoryItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CharacterId, x.Slot }).IsUnique();
            e.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Plant>(e =>
        {
            e.HasKey(x => new { x.ZoneId, x.Id });
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => new { x.ZoneId, x.X, x.Y }).IsUnique();
            e.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        b.Entity<TutorialCompletion>(e =>
        {
            e.HasKey(x => new { x.UserId, x.TutorialId });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => x.Username).IsUnique();
        });

        b.Entity<Character>(e =>
        {
            e.ToTable("Characters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(x => x.User)
                .WithMany(x => x.Characters)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UserId).IsUnique();
        });

        b.Entity<LoginToken>(e =>
        {
            e.ToTable("LoginTokens");
            e.HasKey(x => x.Token);
            e.Property(x => x.Token).HasMaxLength(127);
            e.HasOne(x => x.User)
                .WithMany(x => x.LoginTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ExpiresAt);
        });
    }
}

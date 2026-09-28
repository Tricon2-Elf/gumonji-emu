using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace gumonji.Common.DAL;

public sealed class MainContextFactory : IDesignTimeDbContextFactory<MainContext>
{
    public MainContext CreateDbContext(string[] args)
    {
        var databasePath = args.FirstOrDefault() ?? "gumonji.db";
        var options = new DbContextOptionsBuilder<MainContext>()
            .UseSqlite($"Data Source={Path.GetFullPath(databasePath)}")
            .Options;
        return new MainContext(options);
    }
}

using Microsoft.EntityFrameworkCore;
using UpdateManager.Database.Entities;

namespace UpdateManager.Database.Context;

public class ManagerContext : DbContext
{
    public ManagerContext() {}

    public ManagerContext(DbContextOptions<ManagerContext> options) : base(options) {}

    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.UseSqlite($"Data Source={Path.Join(Directory.GetCurrentDirectory(),"Data", "updatehandler.sqlite")}");
    }
}
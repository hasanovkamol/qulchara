using Microsoft.EntityFrameworkCore;
using OpenBudget.Domain.Entities;
using System.Reflection;

namespace OpenBudget.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Vote> Votes { get; set; } = null!;
    public DbSet<TelegramGroup> TelegramGroups { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

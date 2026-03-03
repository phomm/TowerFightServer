using TowerFight.BusinessLogic.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace TowerFight.BusinessLogic.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<LeaderDao> Leaders { get; init; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<LeaderDao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });
    }
}
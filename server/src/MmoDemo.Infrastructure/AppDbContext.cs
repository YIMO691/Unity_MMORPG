using Microsoft.EntityFrameworkCore;

namespace MmoDemo.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PlayerRow> Players => Set<PlayerRow>();
    public DbSet<PlayerSessionRow> Sessions => Set<PlayerSessionRow>();
    public DbSet<RoleRow> Roles => Set<RoleRow>();
    public DbSet<InventoryItemRow> InventoryItems => Set<InventoryItemRow>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<PlayerRow>(e =>
        {
            e.HasKey(p => p.PlayerId);
            e.Property(p => p.DeviceId).IsRequired().HasMaxLength(128);
            e.Property(p => p.Platform).IsRequired().HasMaxLength(32);
        });

        model.Entity<PlayerSessionRow>(e =>
        {
            e.HasKey(s => s.Token);
            e.Property(s => s.PlayerId).IsRequired().HasMaxLength(64);
            e.HasIndex(s => s.PlayerId);
        });

        model.Entity<RoleRow>(e =>
        {
            e.HasKey(r => r.RoleId);
            e.Property(r => r.PlayerId).IsRequired().HasMaxLength(64);
            e.Property(r => r.Name).IsRequired().HasMaxLength(16);
            e.HasIndex(r => r.PlayerId);
        });

        model.Entity<InventoryItemRow>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.PlayerId).IsRequired().HasMaxLength(64);
            e.Property(i => i.Name).IsRequired().HasMaxLength(64);
            e.Property(i => i.Type).IsRequired().HasMaxLength(16);
            e.HasIndex(i => i.PlayerId);
        });
    }
}

public class PlayerRow
{
    public string PlayerId { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string Platform { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PlayerSessionRow
{
    public string Token { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RoleRow
{
    public string RoleId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Level { get; set; } = 1;
    public int ClassId { get; set; } = 1;
    public long Gold { get; set; } = 100;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class InventoryItemRow
{
    public int Id { get; set; }
    public string PlayerId { get; set; } = "";
    public int TemplateId { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public int SlotIndex { get; set; }
    public bool IsEquipped { get; set; }
}

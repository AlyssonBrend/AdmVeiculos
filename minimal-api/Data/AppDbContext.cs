using Microsoft.EntityFrameworkCore;
using ParkingControl.Domain;

namespace ParkingControl.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ParkingRecord> ParkingRecords { get; set; }
    public DbSet<PricingConfig> PricingConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasKey(v => v.Plate);
            e.Property(v => v.Plate).HasMaxLength(10);
            e.Property(v => v.Model).HasMaxLength(100);
            e.Property(v => v.Color).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("Operator");
        });

        modelBuilder.Entity<ParkingRecord>(e =>
        {
            e.HasOne(r => r.Vehicle)
             .WithMany(v => v.ParkingRecords)
             .HasForeignKey(r => r.Plate)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(r => r.TotalAmount).HasColumnType("decimal(10,2)");
            e.Ignore(r => r.IsActive);
        });

        modelBuilder.Entity<PricingConfig>(e =>
        {
            e.Property(p => p.HourlyRate).HasColumnType("decimal(10,2)");
            e.Property(p => p.DailyMaxRate).HasColumnType("decimal(10,2)");
        });
    }
}

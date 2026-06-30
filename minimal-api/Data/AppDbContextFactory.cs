using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ParkingControl.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Usado apenas para migrations em design-time — nunca vai para produção
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Server=localhost;Port=3306;Database=parking_control;User=root;Password=root;";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;
        return new AppDbContext(options);
    }
}

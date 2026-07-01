using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.Entidades;
namespace MinimalApi.Infraestrutura.DB; 
public class DbContexto : DbContext
{
    private readonly IConfiguration _configuration;
    public DbContexto(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public DbSet<Administrador> Administradores { get; set; } = default!;
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var stringConexao = _configuration.GetConnectionString("mysql");
        if (string.IsNullOrEmpty(stringConexao))
        {
          optionsBuilder.UseMySQL(_configuration.GetConnectionString("Default"),
                    ServerVersion.AutoDetect(_configuration.GetConnectionString("Default")));  
        }

    }
}
//parei no minuto 25
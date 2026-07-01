namespace MinimalApi.Dominio.Entidades;
public class Administrador
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id {get; set; }
    [Required]
    [StringLength(250)]
    public string Email {get; set; } = default!;
    [StringLength(50)]    
    public string Senha {get; set; } = default!;
    [StringLength(10)]
    public string Perfil {get; set; } = default!;
}
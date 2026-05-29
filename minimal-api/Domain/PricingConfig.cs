namespace ParkingControl.Domain;

public class PricingConfig
{
    public int Id { get; set; }
    public decimal HourlyRate { get; set; } = 10.00m;       // Valor por hora
    public int ToleranceMinutes { get; set; } = 15;          // Minutos grátis
    public decimal DailyMaxRate { get; set; } = 80.00m;      // Teto diário
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

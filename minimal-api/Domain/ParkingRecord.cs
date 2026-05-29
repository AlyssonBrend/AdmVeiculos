namespace ParkingControl.Domain;

public class ParkingRecord
{
    public int Id { get; set; }
    public string Plate { get; set; } = string.Empty;
    public Vehicle? Vehicle { get; set; }
    public DateTime EntryTime { get; set; } = DateTime.UtcNow;
    public DateTime? ExitTime { get; set; }
    public decimal? TotalAmount { get; set; }
    public int? OperatorId { get; set; }
    public string Notes { get; set; } = string.Empty;

    // Computed - não persistido
    public bool IsActive => ExitTime == null;
}

namespace ParkingControl.Domain;

public class Vehicle
{
    public string Plate { get; set; } = string.Empty;   // PK - obrigatório
    public string Model { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerPhone { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public ICollection<ParkingRecord> ParkingRecords { get; set; } = [];
}

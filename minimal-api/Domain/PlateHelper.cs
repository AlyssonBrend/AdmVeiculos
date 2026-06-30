namespace ParkingControl.Domain;

public static class PlateHelper
{
    public static string Normalize(string plate) =>
        plate.ToUpperInvariant().Replace("-", "").Replace(" ", "");
}

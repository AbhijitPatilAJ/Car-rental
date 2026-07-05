namespace CarRental.Api.Models;

public class VehicleResult
{
    public string          VehicleId          { get; set; } = string.Empty;
    public string          VehicleName        { get; set; } = string.Empty;  // e.g. "Hyundai Creta"
    public string          Provider           { get; set; } = string.Empty;
    public VehicleCategory Category           { get; set; }
    public decimal         DailyRate          { get; set; }
    public decimal         TotalPrice         { get; set; }
    public int             RentalDays         { get; set; }
    public string          InsuranceType      { get; set; } = string.Empty;
    public string          CancellationPolicy { get; set; } = string.Empty;
    public bool            IsAvailable        { get; set; } = true;
}

namespace CarRental.Api.Models;

public class SearchRequest
{
    public string Pickup { get; set; } = string.Empty;
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public VehicleCategory? Category { get; set; }
}

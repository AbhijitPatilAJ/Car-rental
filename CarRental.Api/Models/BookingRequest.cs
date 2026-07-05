using System.Text.Json.Serialization;

namespace CarRental.Api.Models;

public class BookingRequest
{
    public string    VehicleId          { get; set; } = string.Empty;
    public string    Provider           { get; set; } = string.Empty;
    public string    Pickup             { get; set; } = string.Empty;
    public DateOnly  From               { get; set; }
    public DateOnly  To                 { get; set; }
    public decimal   TotalPrice         { get; set; }   // always in INR (base currency)
    public string    InsuranceType      { get; set; } = string.Empty;
    public string    CancellationPolicy { get; set; } = string.Empty;
    public string    DriverName         { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DocumentType DocumentType { get; set; }

    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Currency selected by the traveller at booking time.
    /// "INR" for domestic; "INR" or "USD" for international pickups.
    /// </summary>
    public string Currency { get; set; } = "INR";
}

namespace CarRental.Api.Models;

public class BookingResult
{
    public string   ReferenceNumber    { get; set; } = string.Empty;
    public string   Provider           { get; set; } = string.Empty;
    public string   VehicleId          { get; set; } = string.Empty;
    public string   Pickup             { get; set; } = string.Empty;
    public DateOnly From               { get; set; }
    public DateOnly To                 { get; set; }

    /// <summary>Total price always stored in INR (base currency).</summary>
    public decimal  TotalPrice         { get; set; }

    /// <summary>Currency the traveller selected at booking time: "INR" or "USD".</summary>
    public string   Currency           { get; set; } = "INR";

    /// <summary>
    /// Exchange rate used at booking time (INR per 1 USD).
    /// 0 when currency is INR (no conversion applied).
    /// </summary>
    public decimal  ExchangeRate       { get; set; }

    /// <summary>TotalPrice converted to the selected currency. Same as TotalPrice when Currency = INR.</summary>
    public decimal  TotalPriceConverted { get; set; }

    public string   InsuranceType      { get; set; } = string.Empty;
    public string   CancellationPolicy { get; set; } = string.Empty;
    public string   DriverName         { get; set; } = string.Empty;
    public string   DocumentType       { get; set; } = string.Empty;
    public string   DocumentNumber     { get; set; } = string.Empty;
    public DateTime ConfirmedAt        { get; set; }
}

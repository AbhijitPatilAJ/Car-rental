using System.Collections.Concurrent;
using CarRental.Api.Models;

namespace CarRental.Api.Data;

/// <summary>
/// In-memory booking store — no database required.
/// Runs 100% offline. All bookings are held in a thread-safe dictionary
/// for the lifetime of the application process.
///
/// Currency note:
///   All prices are stored in INR (base currency).
///   When the traveller selects USD, TotalPriceConverted is calculated using
///   ExchangeRateInr (fixed for demo; replace with live rate API in production).
/// </summary>
public class BookingRepository
{
    // Fixed exchange rate: 1 USD = 84 INR (demo rate — update as needed)
    public const decimal ExchangeRateInr = 84.00m;  // INR per 1 USD

    // Thread-safe dictionary: referenceNumber → BookingResult
    private static readonly ConcurrentDictionary<string, BookingResult> _store
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Saves a confirmed booking. Converts TotalPrice to the selected currency
    /// and records the exchange rate used at the time of booking.
    /// </summary>
    public Task<BookingResult> SaveAsync(BookingRequest request)
    {
        var reference   = GenerateReference(request.From);
        var confirmedAt = DateTime.UtcNow;
        var currency    = string.IsNullOrWhiteSpace(request.Currency) ? "INR"
                          : request.Currency.ToUpperInvariant();

        // Convert: TotalPrice is always INR internally
        decimal exchangeRate       = currency == "USD" ? ExchangeRateInr : 0m;
        decimal totalPriceConverted = currency == "USD"
            ? Math.Round(request.TotalPrice / ExchangeRateInr, 2)
            : request.TotalPrice;

        var result = new BookingResult
        {
            ReferenceNumber     = reference,
            Provider            = request.Provider,
            VehicleId           = request.VehicleId,
            Pickup              = request.Pickup,
            From                = request.From,
            To                  = request.To,
            TotalPrice          = request.TotalPrice,          // always INR
            Currency            = currency,
            ExchangeRate        = exchangeRate,
            TotalPriceConverted = totalPriceConverted,
            InsuranceType       = request.InsuranceType,
            CancellationPolicy  = request.CancellationPolicy,
            DriverName          = request.DriverName,
            DocumentType        = request.DocumentType.ToString(),
            DocumentNumber      = request.DocumentNumber,
            ConfirmedAt         = confirmedAt
        };

        _store[reference] = result;
        return Task.FromResult(result);
    }

    /// <summary>
    /// Retrieves a booking by reference number. Returns null if not found.
    /// </summary>
    public Task<BookingResult?> GetByReferenceAsync(string reference)
    {
        _store.TryGetValue(reference, out var result);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Generates a human-readable unique reference: SKY-{YYYYMMDD}-{4 random uppercase chars}.
    /// </summary>
    internal static string GenerateReference(DateOnly from)
    {
        var datePart   = from.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"SKY-{datePart}-{randomPart}";
    }
}

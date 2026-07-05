using CarRental.Api.Interfaces;
using CarRental.Api.Models;

namespace CarRental.Api.Providers;

/// <summary>
/// PremiumDrive stub provider.
/// Pricing: Flat daily rate — TotalPrice = DailyRate × rental nights.
/// Always available. Comprehensive insurance. Free cancellation up to 48h.
/// 12 vehicles — 3 per category — giving travellers genuine choice within each segment.
/// </summary>
public class PremiumDriveProvider : ICarRentalProvider
{
    public string ProviderName => "PremiumDrive";

    private const string Insurance    = "Comprehensive";
    private const string Cancellation = "Free cancellation up to 48h before pickup";

    // Deterministic catalogue — same input always gives same output.
    // 3 tiers per category: entry / mid / premium within the segment.
    private static readonly List<(string Id, VehicleCategory Category, decimal DailyRate, string Name)> Catalogue =
    [
        // ── Economy ───────────────────────────────────────────────────────────
        ("PD-ECO-001", VehicleCategory.Economy,  45.00m, "Maruti Swift"),
        ("PD-ECO-002", VehicleCategory.Economy,  52.00m, "Hyundai i20"),
        ("PD-ECO-003", VehicleCategory.Economy,  58.00m, "Tata Altroz"),

        // ── Compact ───────────────────────────────────────────────────────────
        ("PD-COM-001", VehicleCategory.Compact,  65.00m, "Honda City"),
        ("PD-COM-002", VehicleCategory.Compact,  72.00m, "Hyundai Verna"),
        ("PD-COM-003", VehicleCategory.Compact,  80.00m, "VW Virtus"),

        // ── SUV ───────────────────────────────────────────────────────────────
        ("PD-SUV-001", VehicleCategory.SUV,      90.00m, "Tata Nexon"),
        ("PD-SUV-002", VehicleCategory.SUV,     105.00m, "Hyundai Creta"),
        ("PD-SUV-003", VehicleCategory.SUV,     125.00m, "Mahindra XUV700"),

        // ── Minivan ───────────────────────────────────────────────────────────
        ("PD-MIN-001", VehicleCategory.Minivan, 110.00m, "Toyota Innova"),
        ("PD-MIN-002", VehicleCategory.Minivan, 130.00m, "Kia Carnival"),
        ("PD-MIN-003", VehicleCategory.Minivan, 155.00m, "Mercedes V-Class"),
    ];

    public Task<IEnumerable<VehicleResult>> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        int nights = request.To.DayNumber - request.From.DayNumber;

        var results = Catalogue
            .Where(v => request.Category is null || v.Category == request.Category)
            .Select(v => new VehicleResult
            {
                VehicleId          = v.Id,
                VehicleName        = v.Name,
                Provider           = ProviderName,
                Category           = v.Category,
                DailyRate          = v.DailyRate,
                TotalPrice         = CalculateTotal(v.DailyRate, nights),
                RentalDays         = nights,
                InsuranceType      = Insurance,
                CancellationPolicy = Cancellation,
                IsAvailable        = true
            });

        return Task.FromResult<IEnumerable<VehicleResult>>(results);
    }

    /// <summary>
    /// Flat rate: total = daily rate × number of nights.
    /// </summary>
    internal static decimal CalculateTotal(decimal dailyRate, int nights)
        => dailyRate * nights;
}

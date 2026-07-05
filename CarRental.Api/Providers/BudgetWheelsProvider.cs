using CarRental.Api.Interfaces;
using CarRental.Api.Models;

namespace CarRental.Api.Providers;

/// <summary>
/// BudgetWheels stub provider.
/// Pricing: Base daily rate + 20% surcharge on Friday, Saturday, and Sunday nights.
/// Surcharge calculated by iterating each rental night — NOT by multiplying daily rate × days.
/// Vehicles with available=false are filtered before returning (per spec).
/// Basic insurance only. Non-refundable cancellation.
/// 16 vehicles — 4 per category (3 available + 1 unavailable) = 12 returned.
/// </summary>
public class BudgetWheelsProvider : ICarRentalProvider
{
    public string ProviderName => "BudgetWheels";

    private const string  Insurance          = "Basic";
    private const string  Cancellation       = "Non-refundable";
    private const decimal WeekendMultiplier  = 1.20m;

    // Deterministic catalogue — 4 per category; last entry in each group is unavailable (tests filtering)
    private static readonly List<(string Id, VehicleCategory Category, decimal DailyRate, bool Available, string Name)> Catalogue =
    [
        // ── Economy ───────────────────────────────────────────────────────────
        ("BW-ECO-001", VehicleCategory.Economy,  35.00m, true,  "Maruti WagonR"),
        ("BW-ECO-002", VehicleCategory.Economy,  38.00m, true,  "Tata Tiago"),
        ("BW-ECO-003", VehicleCategory.Economy,  42.00m, true,  "Renault Kwid"),
        ("BW-ECO-004", VehicleCategory.Economy,  40.00m, false, "Datsun Go"),         // unavailable — filtered

        // ── Compact ───────────────────────────────────────────────────────────
        ("BW-COM-001", VehicleCategory.Compact,  50.00m, true,  "Maruti Dzire"),
        ("BW-COM-002", VehicleCategory.Compact,  55.00m, true,  "Honda Amaze"),
        ("BW-COM-003", VehicleCategory.Compact,  60.00m, true,  "Hyundai Aura"),
        ("BW-COM-004", VehicleCategory.Compact,  53.00m, false, "Tata Tigor"),        // unavailable — filtered

        // ── SUV ───────────────────────────────────────────────────────────────
        ("BW-SUV-001", VehicleCategory.SUV,      68.00m, true,  "Maruti Brezza"),
        ("BW-SUV-002", VehicleCategory.SUV,      75.00m, true,  "Kia Seltos"),
        ("BW-SUV-003", VehicleCategory.SUV,      82.00m, true,  "Skoda Kushaq"),
        ("BW-SUV-004", VehicleCategory.SUV,      70.00m, false, "MG Astor"),          // unavailable — filtered

        // ── Minivan ───────────────────────────────────────────────────────────
        ("BW-MIN-001", VehicleCategory.Minivan,  88.00m, true,  "Maruti Ertiga"),
        ("BW-MIN-002", VehicleCategory.Minivan,  95.00m, true,  "Toyota Rumion"),
        ("BW-MIN-003", VehicleCategory.Minivan, 102.00m, true,  "Mahindra Marazzo"),
        ("BW-MIN-004", VehicleCategory.Minivan,  90.00m, false, "Force Traveller"),   // unavailable — filtered
    ];

    public Task<IEnumerable<VehicleResult>> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        int nights = request.To.DayNumber - request.From.DayNumber;

        var results = Catalogue
            .Where(v => v.Available)                                // filter unavailable — per spec
            .Where(v => request.Category is null || v.Category == request.Category)
            .Select(v => new VehicleResult
            {
                VehicleId          = v.Id,
                VehicleName        = v.Name,
                Provider           = ProviderName,
                Category           = v.Category,
                DailyRate          = v.DailyRate,
                TotalPrice         = CalculateTotalWithSurcharge(v.DailyRate, request.From, request.To),
                RentalDays         = nights,
                InsuranceType      = Insurance,
                CancellationPolicy = Cancellation,
                IsAvailable        = true
            });

        return Task.FromResult<IEnumerable<VehicleResult>>(results);
    }

    /// <summary>
    /// Iterates each rental night and applies 20% surcharge on Friday, Saturday, Sunday.
    /// Per spec: do NOT calculate by multiplying daily rate × number of days.
    /// </summary>
    internal static decimal CalculateTotalWithSurcharge(decimal dailyRate, DateOnly from, DateOnly to)
    {
        decimal total = 0m;

        // Each iteration represents one overnight stay
        // e.g., check-in Aug 1, check-out Aug 7 → nights: Aug 1, 2, 3, 4, 5, 6
        for (var night = from; night < to; night = night.AddDays(1))
        {
            bool isWeekendNight = night.DayOfWeek is DayOfWeek.Friday
                                                   or DayOfWeek.Saturday
                                                   or DayOfWeek.Sunday;

            total += isWeekendNight ? dailyRate * WeekendMultiplier : dailyRate;
        }

        return Math.Round(total, 2);
    }
}

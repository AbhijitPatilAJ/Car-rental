using CarRental.Api.Providers;
using Xunit;

namespace CarRental.Tests;

/// <summary>
/// Tests for BudgetWheels weekend surcharge pricing.
/// Key rule: iterate each rental night, apply 20% surcharge on Fri/Sat/Sun.
/// Do NOT test by multiplying daily rate × number of days.
/// </summary>
public class PricingTests
{
    private const decimal Rate = 100.00m; // simple rate for easy manual verification

    // ── BudgetWheels Weekend Surcharge Tests ──────────────────────────────────

    [Fact]
    public void BudgetWheels_AllWeekdayNights_NoSurcharge()
    {
        // Monday 2025-08-04 → Friday 2025-08-08 = 4 nights (Mon, Tue, Wed, Thu)
        var from = new DateOnly(2025, 8, 4);
        var to   = new DateOnly(2025, 8, 8);

        var total = BudgetWheelsProvider.CalculateTotalWithSurcharge(Rate, from, to);

        // 4 weekday nights × £100 = £400
        Assert.Equal(400.00m, total);
    }

    [Fact]
    public void BudgetWheels_AllWeekendNights_SurchargeOnEveryNight()
    {
        // Friday 2025-08-01 → Monday 2025-08-04 = 3 nights (Fri, Sat, Sun)
        var from = new DateOnly(2025, 8, 1);
        var to   = new DateOnly(2025, 8, 4);

        var total = BudgetWheelsProvider.CalculateTotalWithSurcharge(Rate, from, to);

        // 3 weekend nights × £100 × 1.20 = £360
        Assert.Equal(360.00m, total);
    }

    [Fact]
    public void BudgetWheels_MixedWeekAndWeekend_SurchargeOnlyOnWeekendNights()
    {
        // Monday 2025-08-04 → Sunday 2025-08-10 = 6 nights (Mon, Tue, Wed, Thu, Fri, Sat)
        // Fri = weekend (Aug 8), Sat = weekend (Aug 9), Mon-Thu = weekday
        var from = new DateOnly(2025, 8, 4);
        var to   = new DateOnly(2025, 8, 10);

        var total = BudgetWheelsProvider.CalculateTotalWithSurcharge(Rate, from, to);

        // 4 weekday nights × £100 = £400
        // 2 weekend nights × £100 × 1.20 = £240
        // Total = £640
        Assert.Equal(640.00m, total);
    }

    [Fact]
    public void BudgetWheels_SingleNightWeekday_BaseRateReturned()
    {
        // Wednesday 2025-08-06 → Thursday 2025-08-07 = 1 night (Wed)
        var from = new DateOnly(2025, 8, 6);
        var to   = new DateOnly(2025, 8, 7);

        var total = BudgetWheelsProvider.CalculateTotalWithSurcharge(Rate, from, to);

        Assert.Equal(100.00m, total);
    }

    [Fact]
    public void BudgetWheels_SingleNightWeekend_SurchargeApplied()
    {
        // Saturday 2025-08-02 → Sunday 2025-08-03 = 1 night (Sat)
        var from = new DateOnly(2025, 8, 2);
        var to   = new DateOnly(2025, 8, 3);

        var total = BudgetWheelsProvider.CalculateTotalWithSurcharge(Rate, from, to);

        // £100 × 1.20 = £120
        Assert.Equal(120.00m, total);
    }

    [Fact]
    public void BudgetWheels_TwoWeekRental_CorrectTotalWithMultipleWeekends()
    {
        // Monday 2025-08-04 → Monday 2025-08-18 = 14 nights
        // Week 1: Mon-Thu (4 weekday) + Fri, Sat, Sun (3 weekend)
        // Week 2: Mon-Thu (4 weekday) + Fri, Sat, Sun (3 weekend)
        var from = new DateOnly(2025, 8, 4);
        var to   = new DateOnly(2025, 8, 18);

        var total = BudgetWheelsProvider.CalculateTotalWithSurcharge(Rate, from, to);

        // 8 weekday nights × £100 = £800
        // 6 weekend nights × £120 = £720
        // Total = £1,520
        Assert.Equal(1520.00m, total);
    }

    [Fact]
    public void BudgetWheels_SundayNight_IsConsideredWeekend()
    {
        // Sunday 2025-08-03 → Monday 2025-08-04 = 1 night (Sun)
        var from = new DateOnly(2025, 8, 3);
        var to   = new DateOnly(2025, 8, 4);

        var total = BudgetWheelsProvider.CalculateTotalWithSurcharge(Rate, from, to);

        // Sunday is a weekend night — surcharge applies
        Assert.Equal(120.00m, total);
    }

    [Fact]
    public void BudgetWheels_FridayNight_IsConsideredWeekend()
    {
        // Friday 2025-08-01 → Saturday 2025-08-02 = 1 night (Fri)
        var from = new DateOnly(2025, 8, 1);
        var to   = new DateOnly(2025, 8, 2);

        var total = BudgetWheelsProvider.CalculateTotalWithSurcharge(Rate, from, to);

        // Friday is a weekend night — surcharge applies
        Assert.Equal(120.00m, total);
    }

    // ── PremiumDrive Flat Rate Tests ──────────────────────────────────────────

    [Fact]
    public void PremiumDrive_SevenNights_TotalIsDailyRateTimesNights()
    {
        var total = PremiumDriveProvider.CalculateTotal(Rate, 7);
        Assert.Equal(700.00m, total);
    }

    [Fact]
    public void PremiumDrive_OneNight_TotalEqualsDailyRate()
    {
        var total = PremiumDriveProvider.CalculateTotal(Rate, 1);
        Assert.Equal(100.00m, total);
    }

    [Fact]
    public void PremiumDrive_WeekendIncludes_SameRateAsWeekday()
    {
        // PremiumDrive has no weekend surcharge
        // Fri-Mon (2 nights: Fri, Sat, Sun → actually 3 nights)
        // Flat rate: no surcharge regardless of day
        var from = new DateOnly(2025, 8, 1);  // Friday
        var to   = new DateOnly(2025, 8, 4);  // Monday = 3 nights
        int nights = to.DayNumber - from.DayNumber;

        var total = PremiumDriveProvider.CalculateTotal(Rate, nights);

        // Always flat: 3 × £100 = £300 (no premium for weekend)
        Assert.Equal(300.00m, total);
    }
}

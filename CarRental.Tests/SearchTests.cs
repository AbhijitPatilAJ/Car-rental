using CarRental.Api.Interfaces;
using CarRental.Api.Models;
using CarRental.Api.Providers;
using CarRental.Api.Services;
using Xunit;

namespace CarRental.Tests;

/// <summary>
/// Tests for search aggregation:
/// - PremiumDrive returns 12 vehicles (3 per category), always available
/// - BudgetWheels returns 12 available vehicles (3 per category, 4 filtered)
/// - Category filter works correctly
/// - Service aggregates both providers (24 total)
/// </summary>
public class SearchTests
{
    private readonly DateOnly _from = new(2025, 8, 4);   // Monday
    private readonly DateOnly _to   = new(2025, 8, 11);  // Monday (7 nights)

    // ── PremiumDrive Search Tests ─────────────────────────────────────────────

    [Fact]
    public async Task PremiumDrive_NoCategory_Returns12Vehicles()
    {
        var provider = new PremiumDriveProvider();
        var request  = new SearchRequest { Pickup = "Bangalore", From = _from, To = _to };

        var results = await provider.SearchAsync(request);

        Assert.Equal(12, results.Count());
    }

    [Fact]
    public async Task PremiumDrive_AlwaysReturnsAvailableVehicles()
    {
        var provider = new PremiumDriveProvider();
        var request  = new SearchRequest { Pickup = "Paris", From = _from, To = _to };

        var results = await provider.SearchAsync(request);

        Assert.All(results, v => Assert.True(v.IsAvailable));
    }

    [Fact]
    public async Task PremiumDrive_CategoryFilter_ReturnsOnly3Vehicles()
    {
        var provider = new PremiumDriveProvider();
        var request  = new SearchRequest { Pickup = "Mumbai", From = _from, To = _to, Category = VehicleCategory.SUV };

        var results = await provider.SearchAsync(request);

        Assert.Equal(3, results.Count()); // 3 SUVs in PremiumDrive
        Assert.All(results, v => Assert.Equal(VehicleCategory.SUV, v.Category));
    }

    [Fact]
    public async Task PremiumDrive_AllVehiclesHaveVehicleName()
    {
        var provider = new PremiumDriveProvider();
        var request  = new SearchRequest { Pickup = "Delhi", From = _from, To = _to };

        var results = await provider.SearchAsync(request);

        Assert.All(results, v => Assert.False(string.IsNullOrWhiteSpace(v.VehicleName)));
    }

    [Fact]
    public async Task PremiumDrive_HasCorrectInsuranceAndCancellation()
    {
        var provider = new PremiumDriveProvider();
        var request  = new SearchRequest { Pickup = "Bangalore", From = _from, To = _to };

        var results = await provider.SearchAsync(request);

        Assert.All(results, v =>
        {
            Assert.Equal("Comprehensive", v.InsuranceType);
            Assert.Contains("48h", v.CancellationPolicy);
        });
    }

    [Fact]
    public async Task PremiumDrive_ProviderName_IsPremiumDrive()
    {
        var provider = new PremiumDriveProvider();
        var request  = new SearchRequest { Pickup = "Mumbai", From = _from, To = _to };

        var results = await provider.SearchAsync(request);

        Assert.All(results, v => Assert.Equal("PremiumDrive", v.Provider));
    }

    // ── BudgetWheels Search Tests ─────────────────────────────────────────────

    [Fact]
    public async Task BudgetWheels_NoCategory_Returns12AvailableVehicles()
    {
        // Catalogue: 16 vehicles; 4 unavailable → returns exactly 12
        var provider = new BudgetWheelsProvider();
        var request  = new SearchRequest { Pickup = "Delhi", From = _from, To = _to };

        var results = await provider.SearchAsync(request);

        Assert.Equal(12, results.Count());
    }

    [Fact]
    public async Task BudgetWheels_UnavailableVehicles_AreFilteredOut()
    {
        var provider = new BudgetWheelsProvider();
        var request  = new SearchRequest { Pickup = "Bangalore", From = _from, To = _to };

        var results = await provider.SearchAsync(request);

        // All returned must be available
        Assert.All(results, v => Assert.True(v.IsAvailable));

        // Confirm the 4 unavailable IDs are NOT in results
        var ids = results.Select(v => v.VehicleId).ToHashSet();
        Assert.DoesNotContain("BW-ECO-004", ids);
        Assert.DoesNotContain("BW-COM-004", ids);
        Assert.DoesNotContain("BW-SUV-004", ids);
        Assert.DoesNotContain("BW-MIN-004", ids);
    }

    [Fact]
    public async Task BudgetWheels_CategoryFilter_Returns3AvailableVehicles()
    {
        var provider = new BudgetWheelsProvider();
        var request  = new SearchRequest { Pickup = "Mumbai", From = _from, To = _to, Category = VehicleCategory.Economy };

        var results = await provider.SearchAsync(request);

        Assert.Equal(3, results.Count()); // 3 available Economy in BudgetWheels
        Assert.All(results, v => Assert.Equal(VehicleCategory.Economy, v.Category));
    }

    [Fact]
    public async Task BudgetWheels_AllVehiclesHaveVehicleName()
    {
        var provider = new BudgetWheelsProvider();
        var request  = new SearchRequest { Pickup = "Delhi", From = _from, To = _to };

        var results = await provider.SearchAsync(request);

        Assert.All(results, v => Assert.False(string.IsNullOrWhiteSpace(v.VehicleName)));
    }

    [Fact]
    public async Task BudgetWheels_HasCorrectInsuranceAndCancellation()
    {
        var provider = new BudgetWheelsProvider();
        var request  = new SearchRequest { Pickup = "Bangalore", From = _from, To = _to };

        var results = await provider.SearchAsync(request);

        Assert.All(results, v =>
        {
            Assert.Equal("Basic", v.InsuranceType);
            Assert.Equal("Non-refundable", v.CancellationPolicy);
        });
    }

    // ── CarRentalService Aggregation Tests ────────────────────────────────────

    [Fact]
    public async Task Service_BothProviders_ResultsMerged_24Total()
    {
        var providers = new List<ICarRentalProvider>
        {
            new PremiumDriveProvider(),
            new BudgetWheelsProvider()
        };
        var service = new CarRentalService(providers);
        var request = new SearchRequest { Pickup = "Mumbai", From = _from, To = _to };

        var results = await service.SearchAsync(request);

        // 12 from PremiumDrive + 12 from BudgetWheels
        Assert.Equal(24, results.Count());
    }

    [Fact]
    public async Task Service_BothProviders_AreRepresented()
    {
        var providers = new List<ICarRentalProvider>
        {
            new PremiumDriveProvider(),
            new BudgetWheelsProvider()
        };
        var service = new CarRentalService(providers);
        var request = new SearchRequest { Pickup = "Delhi", From = _from, To = _to };

        var results = await service.SearchAsync(request);

        var providerNames = results.Select(r => r.Provider).Distinct().ToList();
        Assert.Contains("PremiumDrive", providerNames);
        Assert.Contains("BudgetWheels", providerNames);
    }

    [Fact]
    public async Task Service_VehicleResults_HaveCorrectRentalDays()
    {
        var providers = new List<ICarRentalProvider> { new PremiumDriveProvider() };
        var service   = new CarRentalService(providers);
        var request   = new SearchRequest { Pickup = "Bangalore", From = _from, To = _to };

        var results = await service.SearchAsync(request);

        // _from = Aug 4, _to = Aug 11 → 7 nights
        Assert.All(results, v => Assert.Equal(7, v.RentalDays));
    }

    [Fact]
    public async Task Service_WithCategoryFilter_Returns6Vehicles()
    {
        var providers = new List<ICarRentalProvider>
        {
            new PremiumDriveProvider(),
            new BudgetWheelsProvider()
        };
        var service = new CarRentalService(providers);
        var request = new SearchRequest { Pickup = "Mumbai", From = _from, To = _to, Category = VehicleCategory.Minivan };

        var results = await service.SearchAsync(request);

        Assert.Equal(6, results.Count()); // 3 PD Minivans + 3 BW Minivans
        Assert.All(results, v => Assert.Equal(VehicleCategory.Minivan, v.Category));
    }

    [Fact]
    public async Task Service_AllVehiclesHaveVehicleName()
    {
        var providers = new List<ICarRentalProvider>
        {
            new PremiumDriveProvider(),
            new BudgetWheelsProvider()
        };
        var service = new CarRentalService(providers);
        var request = new SearchRequest { Pickup = "Delhi", From = _from, To = _to };

        var results = await service.SearchAsync(request);

        Assert.All(results, v => Assert.False(string.IsNullOrWhiteSpace(v.VehicleName)));
    }
}

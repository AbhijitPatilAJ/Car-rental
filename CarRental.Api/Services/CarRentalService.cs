using CarRental.Api.Interfaces;
using CarRental.Api.Models;

namespace CarRental.Api.Services;

/// <summary>
/// Orchestrates calls to all registered ICarRentalProvider implementations.
/// Calls providers in parallel (Task.WhenAll) and merges results.
/// Adding a new provider requires only a new DI registration — no changes here.
/// </summary>
public class CarRentalService
{
    private readonly IEnumerable<ICarRentalProvider> _providers;

    public CarRentalService(IEnumerable<ICarRentalProvider> providers)
    {
        _providers = providers;
    }

    public async Task<IEnumerable<VehicleResult>> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        // Call all providers in parallel for efficiency
        var tasks = _providers.Select(p => p.SearchAsync(request, ct));
        var results = await Task.WhenAll(tasks);

        // Merge and return unified list
        return results.SelectMany(r => r);
    }
}

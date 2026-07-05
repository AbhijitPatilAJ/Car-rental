using CarRental.Api.Models;

namespace CarRental.Api.Interfaces;

/// <summary>
/// Abstraction over a car rental provider.
/// Implement this interface to add a new provider without changing core flow.
/// Each implementation is responsible for:
///   - Its own pricing calculation
///   - Filtering unavailable vehicles
///   - Returning normalised VehicleResult objects
/// </summary>
public interface ICarRentalProvider
{
    /// <summary>
    /// Provider display name (e.g., "PremiumDrive" or "BudgetWheels").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Returns available vehicles matching the search criteria.
    /// Implementations must filter out unavailable vehicles before returning.
    /// </summary>
    Task<IEnumerable<VehicleResult>> SearchAsync(SearchRequest request, CancellationToken ct = default);
}

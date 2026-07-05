using CarRental.Api.Models;

namespace CarRental.Api.Services;

/// <summary>
/// City registry for document validation.
/// Domestic cities (India): accept National ID or Passport.
/// International cities: require Passport only.
/// </summary>
public static class CityRegistry
{
    public static readonly HashSet<string> DomesticCities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Bangalore",
        "Mumbai",
        "Delhi"
    };

    public static readonly HashSet<string> InternationalCities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Paris",
        "Dubai",
        "New York",
        "Tokyo",
        "Sydney"
    };

    public static PickupLocationType? Classify(string city)
    {
        if (DomesticCities.Contains(city))   return PickupLocationType.Domestic;
        if (InternationalCities.Contains(city)) return PickupLocationType.International;
        return null; // unknown city
    }
}

/// <summary>
/// Validates that the provided document type is acceptable for the pickup location.
/// Returns null if valid, or a ValidationError if not.
/// Both client-side (JS) and this server-side service enforce the same rules.
/// </summary>
public class DocumentValidationService
{
    public ValidationError? Validate(string pickup, DocumentType documentType)
    {
        var locationType = CityRegistry.Classify(pickup);

        if (locationType is null)
        {
            return new ValidationError
            {
                Code    = "UNKNOWN_PICKUP_LOCATION",
                Message = $"The pickup location '{pickup}' is not recognised. " +
                          $"Domestic: {string.Join(", ", CityRegistry.DomesticCities)}. " +
                          $"International: {string.Join(", ", CityRegistry.InternationalCities)}.",
                Field   = "pickup"
            };
        }

        // International pickups require a Passport
        if (locationType == PickupLocationType.International && documentType != DocumentType.Passport)
        {
            return new ValidationError
            {
                Code    = "DOCUMENT_TYPE_MISMATCH",
                Message = $"'{pickup}' is an international pickup location. A Passport is required. National ID is not accepted.",
                Field   = "documentType"
            };
        }

        // Domestic pickups accept both Passport and NationalId — no restriction
        return null;
    }
}

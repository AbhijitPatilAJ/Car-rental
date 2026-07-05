using CarRental.Api.Models;
using CarRental.Api.Services;
using Xunit;

namespace CarRental.Tests;

/// <summary>
/// Tests for document validation rules:
/// - International pickup requires Passport
/// - Domestic pickup (Bangalore, Mumbai, Delhi) accepts both Passport and National ID
/// - Unknown pickup returns UNKNOWN_PICKUP_LOCATION error
/// </summary>
public class ValidationTests
{
    private readonly DocumentValidationService _sut = new();

    // ── International Pickup Tests ────────────────────────────────────────────

    [Theory]
    [InlineData("Paris")]
    [InlineData("Dubai")]
    [InlineData("New York")]
    [InlineData("Tokyo")]
    [InlineData("Sydney")]
    public void International_WithPassport_IsValid(string city)
    {
        var error = _sut.Validate(city, DocumentType.Passport);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("Paris")]
    [InlineData("Dubai")]
    [InlineData("New York")]
    [InlineData("Tokyo")]
    [InlineData("Sydney")]
    public void International_WithNationalId_Returns422Error(string city)
    {
        var error = _sut.Validate(city, DocumentType.NationalId);

        Assert.NotNull(error);
        Assert.Equal("DOCUMENT_TYPE_MISMATCH", error.Code);
        Assert.Equal("documentType", error.Field);
        Assert.Contains("Passport", error.Message);
        Assert.Contains("international", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Domestic Pickup Tests (Indian cities) ─────────────────────────────────

    [Theory]
    [InlineData("Bangalore")]
    [InlineData("Mumbai")]
    [InlineData("Delhi")]
    public void Domestic_WithNationalId_IsValid(string city)
    {
        var error = _sut.Validate(city, DocumentType.NationalId);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("Bangalore")]
    [InlineData("Mumbai")]
    [InlineData("Delhi")]
    public void Domestic_WithPassport_IsAlsoValid(string city)
    {
        // Passport is always accepted — even for domestic pickups
        var error = _sut.Validate(city, DocumentType.Passport);
        Assert.Null(error);
    }

    // ── Unknown City Tests ────────────────────────────────────────────────────

    [Fact]
    public void UnknownCity_ReturnsError_WithUnknownCode()
    {
        var error = _sut.Validate("Atlantis", DocumentType.Passport);

        Assert.NotNull(error);
        Assert.Equal("UNKNOWN_PICKUP_LOCATION", error.Code);
        Assert.Equal("pickup", error.Field);
    }

    [Fact]
    public void UnknownCity_ErrorMessage_ContainsCityName()
    {
        var error = _sut.Validate("Narnia", DocumentType.NationalId);

        Assert.NotNull(error);
        Assert.Contains("Narnia", error.Message);
    }

    // ── City Registry Tests ───────────────────────────────────────────────────

    [Fact]
    public void CityRegistry_CaseInsensitive_Bangalore()
    {
        var locationType = CityRegistry.Classify("bangalore");
        Assert.Equal(PickupLocationType.Domestic, locationType);
    }

    [Fact]
    public void CityRegistry_CaseInsensitive_PARIS()
    {
        var locationType = CityRegistry.Classify("PARIS");
        Assert.Equal(PickupLocationType.International, locationType);
    }

    [Fact]
    public void CityRegistry_Contains3DomesticCities()
    {
        Assert.Equal(3, CityRegistry.DomesticCities.Count);
        Assert.Contains("Bangalore", CityRegistry.DomesticCities, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Mumbai",    CityRegistry.DomesticCities, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Delhi",     CityRegistry.DomesticCities, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void CityRegistry_AtLeast3InternationalCities_Defined()
    {
        Assert.True(CityRegistry.InternationalCities.Count >= 3,
            $"Expected at least 3 international cities, found {CityRegistry.InternationalCities.Count}");
    }
}

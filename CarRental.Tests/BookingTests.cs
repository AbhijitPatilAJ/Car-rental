using CarRental.Api.Data;
using Xunit;

namespace CarRental.Tests;

/// <summary>
/// Tests for booking-related logic:
/// - Reference number generation format
/// - Reference number uniqueness
/// </summary>
public class BookingTests
{
    // ── Reference Number Generation Tests ────────────────────────────────────

    [Fact]
    public void GenerateReference_Format_MatchesSKYPattern()
    {
        var from      = new DateOnly(2025, 8, 1);
        var reference = BookingRepository.GenerateReference(from);

        // Expected format: SKY-20250801-XXXX
        Assert.StartsWith("SKY-20250801-", reference);
        Assert.Equal(17, reference.Length); // SKY- (4) + 8 digits + - (1) + 4 chars = 17
    }

    [Fact]
    public void GenerateReference_RandomPart_IsUppercase()
    {
        var from      = new DateOnly(2025, 8, 1);
        var reference = BookingRepository.GenerateReference(from);

        var randomPart = reference.Split('-')[2]; // "XXXX"
        Assert.Equal(randomPart.ToUpperInvariant(), randomPart);
    }

    [Fact]
    public void GenerateReference_RandomPart_IsFourCharacters()
    {
        var from      = new DateOnly(2025, 8, 1);
        var reference = BookingRepository.GenerateReference(from);

        var parts = reference.Split('-');
        Assert.Equal(3, parts.Length);        // SKY, date, random
        Assert.Equal(4, parts[2].Length);     // 4 random chars
    }

    [Fact]
    public void GenerateReference_TwoCallsForSameDate_ProduceDifferentReferences()
    {
        var from = new DateOnly(2025, 8, 1);

        var ref1 = BookingRepository.GenerateReference(from);
        var ref2 = BookingRepository.GenerateReference(from);

        // Different references for same date (random component ensures uniqueness)
        Assert.NotEqual(ref1, ref2);
    }

    [Fact]
    public void GenerateReference_DifferentDates_ProduceDifferentDateParts()
    {
        var from1 = new DateOnly(2025, 8,  1);
        var from2 = new DateOnly(2025, 12, 25);

        var ref1 = BookingRepository.GenerateReference(from1);
        var ref2 = BookingRepository.GenerateReference(from2);

        Assert.Contains("20250801", ref1);
        Assert.Contains("20251225", ref2);
    }

    [Fact]
    public void GenerateReference_TenCalls_AllUnique()
    {
        // With 65,536 possible 4-char hex suffixes, 10 calls have an
        // astronomically low collision probability (< 0.007%). This is
        // a meaningful uniqueness check for the demo scale.
        var from       = new DateOnly(2025, 8, 1);
        var references = new HashSet<string>();

        for (int i = 0; i < 10; i++)
            references.Add(BookingRepository.GenerateReference(from));

        Assert.Equal(10, references.Count);
    }

    [Fact]
    public void GenerateReference_StartsWithSKY()
    {
        var from      = new DateOnly(2025, 1, 15);
        var reference = BookingRepository.GenerateReference(from);

        Assert.StartsWith("SKY-", reference);
    }
}

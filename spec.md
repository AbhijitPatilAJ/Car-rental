# spec.md — Data Models & Interface Contracts

> **Committed before any implementation files** — defines the contract that all code must conform to.
> Updated to reflect the fully offline in-memory architecture and dual-currency configuration.

---

## Enums

```csharp
public enum VehicleCategory { Economy, Compact, SUV, Minivan }
public enum DocumentType    { Passport, NationalId }
public enum PickupLocationType { Domestic, International }
```

---

## Models

### SearchRequest
```csharp
public class SearchRequest {
    string Pickup;             // required
    DateOnly From;             // required
    DateOnly To;               // required; must be after From
    VehicleCategory? Category; // optional filter
}
```

### VehicleResult (unified response)
```csharp
public class VehicleResult {
    string VehicleId;
    string VehicleName;        // specific model name (e.g., "Hyundai Creta")
    string Provider;           // "PremiumDrive" | "BudgetWheels"
    VehicleCategory Category;
    decimal DailyRate;         // base per-night rate (stored in INR)
    decimal TotalPrice;        // calculated total for the rental period (stored in INR)
    int RentalDays;
    string InsuranceType;      // "Comprehensive" | "Basic"
    string CancellationPolicy; // "Free cancellation up to 48h before pickup" | "Non-refundable"
    bool IsAvailable;          // always true in result list (filtered)
}
```

### BookingRequest
```csharp
public class BookingRequest {
    string VehicleId;       string Provider;
    string Pickup;          DateOnly From;         DateOnly To;
    decimal TotalPrice;     string InsuranceType;  string CancellationPolicy;
    string DriverName;      DocumentType DocumentType; string DocumentNumber;
    string Currency;        // "INR" | "USD"
}
```

### BookingResult
```csharp
public class BookingResult {
    string ReferenceNumber;      // SKY-{YYYYMMDD}-{4chars}
    string Provider;             string VehicleId;
    string Pickup;               DateOnly From;    DateOnly To;
    decimal TotalPrice;          // stored base price in INR
    string Currency;             // traveller's selected currency: "INR" | "USD"
    decimal ExchangeRate;        // conversion rate used (e.g. 84.00 for USD, 0 for INR)
    decimal TotalPriceConverted; // total price in selected currency
    string InsuranceType;        string CancellationPolicy;
    string DriverName;           string DocumentType;  string DocumentNumber;
    DateTime ConfirmedAt;        // UTC
}
```

### ValidationError (422 body)
```csharp
public class ValidationError {
    string Code;     // "DOCUMENT_TYPE_MISMATCH" | "UNKNOWN_PICKUP_LOCATION"
    string Message;  // human-readable
    string Field;    // "documentType" | "pickup"
}
```

---

## ICarRentalProvider Interface

```csharp
public interface ICarRentalProvider
{
    string ProviderName { get; }
    Task<IEnumerable<VehicleResult>> SearchAsync(SearchRequest request, CancellationToken ct = default);
}
```

Adding a third provider = implement this interface + one DI registration. Zero other changes.

---

## API Contracts

### GET /cars/search
**Required**: `pickup`, `from` (YYYY-MM-DD), `to` (YYYY-MM-DD)  
**Optional**: `category` (Economy|Compact|SUV|Minivan)

| Error | Condition |
|-------|-----------|
| 400 | Any required param missing |
| 400 | Invalid date format |
| 400 | `to` not after `from` |
| 400 | Unknown category value |

**200 OK** → `VehicleResult[]` (all available, both providers merged)

### POST /cars/book
**Body**: `BookingRequest` JSON  
**201 Created** → `BookingResult`  
**422** → `ValidationError` (document mismatch)

### GET /cars/booking/{reference}
**200 OK** → `BookingResult`  
**404** → `{ "error": "..." }`

---

## Pricing Rules

### PremiumDrive — Flat Rate
```
TotalPrice = DailyRate × nights
nights = (To.DayNumber - From.DayNumber)
```

### BudgetWheels — Weekend Surcharge
```
WeekendDays = { Friday, Saturday, Sunday }  — 20% more

TotalPrice = 0
for each night from From (inclusive) to To (exclusive):
    if night.DayOfWeek in WeekendDays:
        TotalPrice += DailyRate × 1.20
    else:
        TotalPrice += DailyRate
```

**Do NOT** calculate as `DailyRate × numberOfNights`.

---

## Document Validation

| Pickup Type | Passport | National ID |
|-------------|----------|-------------|
| Domestic | ✅ Accepted | ✅ Accepted |
| International | ✅ Required | ❌ Rejected (422) |

**Domestic cities**: Bangalore, Mumbai, Delhi  
**International cities**: Paris, Dubai, New York, Tokyo, Sydney

---

## Provider Stubs

### PremiumDrive (always available)
12 vehicles total (3 per category: entry, mid, premium).
- *Economy*: Maruti Swift (₹45), Hyundai i20 (₹52), Tata Altroz (₹58)
- *Compact*: Honda City (₹65), Hyundai Verna (₹72), VW Virtus (₹80)
- *SUV*: Tata Nexon (₹90), Hyundai Creta (₹105), Mahindra XUV700 (₹125)
- *Minivan*: Toyota Innova (₹110), Kia Carnival (₹130), Mercedes V-Class (₹155)

### BudgetWheels (12 available, 4 filtered)
16 vehicles total (3 available + 1 unavailable per category).
- *Economy*: Maruti WagonR (₹35), Tata Tiago (₹38), Renault Kwid (₹42), Datsun Go (₹40, unavailable)
- *Compact*: Maruti Dzire (₹50), Honda Amaze (₹55), Hyundai Aura (₹60), Tata Tigor (₹53, unavailable)
- *SUV*: Maruti Brezza (₹68), Kia Seltos (₹75), Skoda Kushaq (₹82), MG Astor (₹70, unavailable)
- *Minivan*: Maruti Ertiga (₹88), Toyota Rumion (₹95), Mahindra Marazzo (₹102), Force Traveller (₹90, unavailable)

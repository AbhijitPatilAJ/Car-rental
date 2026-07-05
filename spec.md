# spec.md — Data Models & Interface Contracts

> **Committed before any implementation files** — defines the contract that all code must conform to.

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
    string Pickup;           // required
    DateOnly From;           // required
    DateOnly To;             // required; must be after From
    VehicleCategory? Category; // optional filter
}
```

### VehicleResult (unified response)
```csharp
public class VehicleResult {
    string VehicleId;
    string Provider;           // "PremiumDrive" | "BudgetWheels"
    VehicleCategory Category;
    decimal DailyRate;         // base per-night rate
    decimal TotalPrice;        // calculated total for the rental period
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
}
```

### BookingResult
```csharp
public class BookingResult {
    string ReferenceNumber;   // SKY-{YYYYMMDD}-{4chars}
    string Provider;          string VehicleId;
    string Pickup;            DateOnly From;    DateOnly To;
    decimal TotalPrice;       string InsuranceType; string CancellationPolicy;
    string DriverName;        string DocumentType;  string DocumentNumber;
    DateTime ConfirmedAt;     // UTC
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

**Domestic cities**: London, Manchester  
**International cities**: Paris, Dubai, New York, Tokyo, Sydney

---

## Provider Stubs

### PremiumDrive (always available)
| VehicleId | Category | DailyRate |
|-----------|----------|-----------|
| PD-ECO-001 | Economy | £45 |
| PD-COM-001 | Compact | £60 |
| PD-SUV-001 | SUV | £85 |
| PD-MIN-001 | Minivan | £100 |

### BudgetWheels (4 available, 4 filtered)
| VehicleId | Category | DailyRate | Available |
|-----------|----------|-----------|-----------|
| BW-ECO-001 | Economy | £38 | ✅ |
| BW-ECO-002 | Economy | £40 | ❌ filtered |
| BW-COM-001 | Compact | £52 | ✅ |
| BW-COM-002 | Compact | £55 | ❌ filtered |
| BW-SUV-001 | SUV | £70 | ✅ |
| BW-SUV-002 | SUV | £72 | ❌ filtered |
| BW-MIN-001 | Minivan | £88 | ✅ |
| BW-MIN-002 | Minivan | £90 | ❌ filtered |

---

## Database Schema

```sql
CREATE TABLE Bookings (
    Id                 INT AUTO_INCREMENT PRIMARY KEY,
    ReferenceNumber    VARCHAR(50)   NOT NULL UNIQUE,
    Provider           VARCHAR(100)  NOT NULL,
    VehicleId          VARCHAR(100)  NOT NULL,
    Pickup             VARCHAR(200)  NOT NULL,
    FromDate           DATE          NOT NULL,
    ToDate             DATE          NOT NULL,
    TotalPrice         DECIMAL(10,2) NOT NULL,
    InsuranceType      VARCHAR(100)  NOT NULL,
    CancellationPolicy VARCHAR(500)  NOT NULL,
    DriverName         VARCHAR(300)  NOT NULL,
    DocumentType       VARCHAR(50)   NOT NULL,
    DocumentNumber     VARCHAR(200)  NOT NULL,
    ConfirmedAt        DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

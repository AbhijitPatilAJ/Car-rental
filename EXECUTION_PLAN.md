# EXECUTION_PLAN.md — Implementation Plan

This execution plan maps out the architecture and structure of the fully offline, in-memory SkyRoute Car Rental platform.

---

## Phase 0 — Workspace Setup & Bootstrapping

```bash
dotnet new sln -n CarRental
dotnet new webapi -n CarRental.Api --framework net8.0 --use-minimal-apis -o CarRental.Api
dotnet new xunit -n CarRental.Tests --framework net8.0 -o CarRental.Tests
dotnet sln add CarRental.Api/CarRental.Api.csproj
dotnet sln add CarRental.Tests/CarRental.Tests.csproj
dotnet add CarRental.Tests reference CarRental.Api/CarRental.Api.csproj
```

### Swagger/OpenAPI Dependencies (in CarRental.Api.csproj)
```bash
dotnet add package Microsoft.AspNetCore.OpenApi --version 8.0.7
dotnet add package Swashbuckle.AspNetCore --version 6.6.2
```

---

## Phase 1 — Core Models & Interface Definition

All domain contracts and interfaces are designed first to support extensibility:
- `CarRental.Api/Models/Enums.cs` — Unified enums (VehicleCategory, DocumentType, PickupLocationType).
- `CarRental.Api/Models/SearchRequest.cs`
- `CarRental.Api/Models/VehicleResult.cs` — Holds `VehicleName` properties for models.
- `CarRental.Api/Models/BookingRequest.cs` — Includes travelers' chosen `Currency`.
- `CarRental.Api/Models/BookingResult.cs` — Captures `Currency`, `ExchangeRate`, and `TotalPriceConverted`.
- `CarRental.Api/Interfaces/ICarRentalProvider.cs` — DI-injectable provider stub base.

---

## Phase 2 — Provider Catalogues & Pricing Implementations

### PremiumDrive
- 12 vehicles total (3 per category).
- Pricing strategy: Flat daily rate (`TotalPrice = DailyRate × nights`).
- Insurance: Comprehensive (included in price), Cancellation: Free 48h before pickup.

### BudgetWheels
- 16 vehicles total (12 available, 4 filtered out to simulate provider unavailability).
- Pricing strategy: Base daily rate + 20% surcharge on Friday, Saturday, and Sunday nights.
- Calculated night-by-night through date loops.
- Insurance: Basic, Cancellation: Non-refundable.

---

## Phase 3 — Core Logic Services

### CarRentalService
- Parallel query aggregator.
- Injects `IEnumerable<ICarRentalProvider>` to execute parallel calls using `Task.WhenAll`.
- Normalizes and merges all results.

### DocumentValidationService
- Matches pickup location to city classification registry.
- **Domestic** (Bangalore, Mumbai, Delhi) → Accept National ID or Passport.
- **International** (Paris, Dubai, New York, Tokyo, Sydney) → Require Passport (rejects National ID with 422).

---

## Phase 4 — Booking Repository (In-Memory)

- Uses a process-lifetime thread-safe `ConcurrentDictionary` to store confirmed bookings.
- Handles USD currency conversions at save-time using a fixed rate (`1 USD = ₹84`).
- Exposes `GenerateReference` using format `SKY-{YYYYMMDD}-{4 random uppercase hex chars}`.

---

## Phase 5 — Minimal API Endpoints (Program.cs)

- Configures Swagger UI at `/swagger` with explicit string serialization for enums.
- Configures CORS policies to allow local frontend access (`file://`).
- Maps endpoints:
  - `GET /` (Health check)
  - `GET /cars/search` (Search vehicles with query validation)
  - `POST /cars/book` (Server-side document validation, save booking)
  - `GET /cars/booking/{ref}` (Retrieve booking details)

---

## Phase 6 — Frontend UI Implementation (skyroute-ui/)

- `skyroute-ui/css/styles.css` — Dark theme system with surcharge details boxes.
- `skyroute-ui/js/api.js` — Client and currency formatters.
- `skyroute-ui/js/search.js` — Live search cards showing dual currency (INR + USD) for international pickups.
- `skyroute-ui/js/booking.js` — Booking checkout with dynamic currency toggles and real-time surcharge display.
- `skyroute-ui/index.html`, `skyroute-ui/booking.html`, `skyroute-ui/confirmation.html`.

---

## Phase 7 — Test Suite (CarRental.Tests/)

Runs 56 xUnit tests using the Arrange-Act-Assert (AAA) pattern:
- `PricingTests.cs` (11 tests) — BudgetWheels surcharge iterations, PremiumDrive flat rates.
- `ValidationTests.cs` (13 tests) — Document mismatch rules, city registry case-sensitivity.
- `SearchTests.cs` (16 tests) — Parallel search merging, category filtering, available status filtering.
- `BookingTests.cs` (16 tests) — Reference uniqueness checks, booking retrieval.

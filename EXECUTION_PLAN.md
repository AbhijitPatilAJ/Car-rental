# EXECUTION_PLAN.md — Implementation Plan

## Phase 0 — Bootstrap

```bash
dotnet new sln -n CarRental
dotnet new webapi -n CarRental.Api --framework net8.0 --use-minimal-apis -o CarRental.Api
dotnet new xunit -n CarRental.Tests --framework net8.0 -o CarRental.Tests
dotnet sln add CarRental.Api/CarRental.Api.csproj
dotnet sln add CarRental.Tests/CarRental.Tests.csproj
dotnet add CarRental.Tests reference CarRental.Api/CarRental.Api.csproj
```

NuGet packages:
```bash
# In CarRental.Api/
dotnet add package MySqlConnector --version 2.3.7
dotnet add package DotNetEnv --version 3.1.1
```

Commit `spec.md` before any code:
```bash
git add spec.md
git commit -m "docs: add spec.md before implementation"
```

## Phase 1 — Database

Create `database/schema.sql` with `CarRentalDb` and `Bookings` table.

Run: `mysql -u root -p < database/schema.sql`

## Phase 2 — Core Models & Interface

Files:
- `CarRental.Api/Models/Enums.cs` — VehicleCategory, DocumentType, PickupLocationType
- `CarRental.Api/Models/SearchRequest.cs`
- `CarRental.Api/Models/VehicleResult.cs`
- `CarRental.Api/Models/BookingRequest.cs`
- `CarRental.Api/Models/BookingResult.cs`
- `CarRental.Api/Models/ValidationError.cs`
- `CarRental.Api/Interfaces/ICarRentalProvider.cs`

## Phase 3 — Provider Implementations

### PremiumDrive
- 4 vehicles, always available
- `TotalPrice = DailyRate × nights`
- Insurance: Comprehensive, Cancellation: Free 48h

### BudgetWheels
- 8 vehicles, 4 unavailable (filtered)
- Day-by-day loop: +20% on Fri/Sat/Sun
- Insurance: Basic, Cancellation: Non-refundable

## Phase 4 — Services

### CarRentalService
- `IEnumerable<ICarRentalProvider>` injected
- `Task.WhenAll` for parallel calls
- Merge and return

### DocumentValidationService
- `CityRegistry` with domestic/international sets (case-insensitive)
- Returns `ValidationError` or `null`

## Phase 5 — BookingRepository

- Raw ADO.NET with MySqlConnector
- `SaveAsync` — insert and return BookingResult
- `GetByReferenceAsync` — SELECT by reference
- `GenerateReference` — `SKY-{YYYYMMDD}-{4chars}` (internal static for testability)

## Phase 6 — Program.cs (Minimal API)

- Load `.env` via DotNetEnv
- Register all DI services
- Map 4 routes: `/`, `/cars/search`, `/cars/book`, `/cars/booking/{ref}`

## Phase 7 — Frontend

Files:
- `skyroute-ui/css/styles.css` — Dark mode design system
- `skyroute-ui/js/api.js` — Shared API client + city validation
- `skyroute-ui/js/search.js` — Search form + card rendering
- `skyroute-ui/js/booking.js` — Booking form + validation
- `skyroute-ui/index.html`
- `skyroute-ui/booking.html`
- `skyroute-ui/confirmation.html`

## Phase 8 — Tests

- `PricingTests.cs` — 11 tests (BudgetWheels surcharge, PremiumDrive flat rate)
- `ValidationTests.cs` — 13 tests (document rules, city registry)
- `SearchTests.cs` — 14 tests (providers, filtering, aggregation)
- `BookingTests.cs` — 7 tests (reference number format and uniqueness)

## Phase 9 — Documentation

- README.md, spec.md (before code), PREREQUISITES.md, prompts.md, reflection.md

## Definition of Done

- [x] spec.md committed before implementation
- [x] All 3 API endpoints respond correctly
- [x] 400 for missing/invalid params
- [x] 422 for document mismatch
- [x] BudgetWheels surcharge day-by-day
- [x] BudgetWheels unavailable vehicles filtered
- [x] Booking flow completes with reference number
- [x] Frontend: loading, results, empty, error, confirmation states
- [x] 51/51 unit tests pass
- [x] Runs from clean clone using only README
- [x] .env not committed (in .gitignore)
- [x] prompts.md documents 10 AI prompts
- [x] reflection.md contains honest analysis

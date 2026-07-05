# prompts.md — AI Prompts & Key Decisions

> Documents all significant AI prompts used during development of the SkyRoute Car Rental feature.
> Each prompt is numbered (Prompt 1, Prompt 2...) and includes the exact text, output summary, and decision made.
> Updated to reflect the current implementation: in-memory store, Swagger/OpenAPI, Indian cities, 24-vehicle catalogue.

---

## Prompt 1 — Project Planning & Architecture

**Phase**: Analysis & Design  
**Tool**: Claude (AI coding assistant)

**Prompt**:
```
I need to build a Car Rental Availability feature for SkyRoute platform.
Requirements:
- Two providers: PremiumDrive (flat rate) and BudgetWheels (base + weekend surcharge)
- Interface ICarRentalProvider with DI-injected stubs
- Third provider must be addable without reworking core
- GET /cars/search, POST /cars/book, GET /cars/booking/{ref}
- Document validation: international = Passport required, domestic = NationalId accepted
- Must run fully offline — no external services
- Plain HTML/JS frontend

Help me design the architecture: which projects, which classes, which interface,
and how data flows from search to booking to confirmation.
```

**Output Summary**:
AI suggested a clean separation: `Interfaces/ICarRentalProvider.cs`, `Providers/` for stubs, `Services/CarRentalService.cs` for aggregation, `Data/BookingRepository.cs` for persistence, and `Program.cs` for Minimal API routes. Recommended `Task.WhenAll` for parallel provider calls.

**Decision**:
Accepted the architecture. Chose in-memory `ConcurrentDictionary` over any database to keep the app truly offline — no MySQL, no connection strings, no setup required. Runs on a clean machine with only `dotnet run`.

---

## Prompt 2 — BudgetWheels Weekend Surcharge Algorithm

**Phase**: Implementation  
**Tool**: Claude

**Prompt**:
```
Implement the BudgetWheels weekend surcharge in C#.
Rules:
- Friday, Saturday, and Sunday nights cost 20% more
- Calculate total by iterating over each rental night
- Do NOT calculate by multiplying daily rate × number of days
- Use DateOnly for from/to dates

Include edge cases: all weekdays, all weekends, mixed, 2-week rentals.
Explain what "night" means — is it the check-in date or the date you sleep?
```

**Output Summary**:
AI correctly defined "night" as the date you sleep (from inclusive, to exclusive). Produced the day-by-day loop checking `DayOfWeek.Friday | Saturday | Sunday`. Showed that for Aug 1 (Fri) to Aug 7, the nights are Aug 1–6 (loop condition: `night < to`).

**Decision**:
Accepted. Verified the math manually: 3 weekend nights + 4 weekday nights for a Mon–Mon 7-night rental. Used `Math.Round(total, 2)` to avoid floating-point artifacts.

---

## Prompt 3 — ICarRentalProvider Interface Design

**Phase**: Design  
**Tool**: Claude

**Prompt**:
```
Design C# interface ICarRentalProvider for a rental aggregator.
Requirements:
- Async (future providers may make real HTTP calls)
- DI-injectable with multiple implementations (IEnumerable<ICarRentalProvider>)
- Each provider handles its own pricing — no shared pricing service
- Adding a 3rd provider = zero changes to existing code
- Include CancellationToken support
```

**Output Summary**:
```csharp
public interface ICarRentalProvider
{
    string ProviderName { get; }
    Task<IEnumerable<VehicleResult>> SearchAsync(SearchRequest request, CancellationToken ct = default);
}
```

**Decision**:
Accepted exactly. The `ct = default` makes CancellationToken optional, simplifying unit tests that call `SearchAsync` directly without a token. Each provider self-contains its pricing logic — adding `LuxuryFleetProvider` requires only a new class + one DI line.

---

## Prompt 4 — Document Validation Logic

**Phase**: Design  
**Tool**: Claude

**Prompt**:
```
The spec says:
- "International pickup location → Passport required"
- "Domestic pickup location → National ID accepted"

Question: Does "National ID accepted" mean Passport is REJECTED for domestic?
Or does it mean Passport is also valid (it's always stronger)?

Design the validation logic precisely. I need it in both C# (server) and JS (client).
Domestic cities are: Bangalore, Mumbai, Delhi (India).
International cities are: Paris, Dubai, New York, Tokyo, Sydney.
```

**Output Summary**:
AI correctly interpreted: "accepted" means NationalId is a valid option, not that Passport is banned. Domestic = accept either; International = Passport only. Same rule mirrored in both C# `DocumentValidationService` and JavaScript `validateDocument()` in `api.js`.

**Decision**:
Accepted. Passport is always sufficient — you wouldn't reject someone's passport at a domestic desk. The `CityRegistry` uses `HashSet<string>` with `StringComparer.OrdinalIgnoreCase` for case-insensitive matching. Validation is dual-layer: client-side JS pre-empts the round trip; server-side C# enforces correctness regardless of what the frontend does.

---

## Prompt 5 — Swagger / OpenAPI Configuration

**Phase**: Implementation  
**Tool**: Claude

**Prompt**:
```
Add Swagger/OpenAPI to a .NET 8 Minimal API.
Requirements per CLAUDE.md:
- Every endpoint must have .WithOpenApi()
- Define exact responses: 200 OK, 400 BadRequest, 422 UnprocessableEntity, 404 NotFound
- Swagger UI accessible at http://localhost:5000/swagger
- Enum values (VehicleCategory, DocumentType) must render as strings, not integers
- Inline parameter descriptions so testers understand pickup cities, date formats, etc.
- Works offline — no auth, no external CDN for Swagger assets
```

**Output Summary**:
AI configured `Swashbuckle.AspNetCore` with `AddSwaggerGen` → `UseSwagger` → `UseSwaggerUI`. Each endpoint decorated with:
```csharp
.WithName("SearchVehicles")
.WithTags("Cars")
.WithOpenApi(o => { o.Summary = "..."; o.Description = "..."; return o; })
.Produces<IEnumerable<VehicleResult>>(200)
.ProducesProblem(400)
```
Used `c.UseInlineDefinitionsForEnums()` to force string rendering. `DefaultModelsExpandDepth(2)` and `DisplayRequestDuration()` improve Swagger UX.

**Decision**:
Accepted. Added inline markdown to `.Description` (including bullet lists and bold city names) so the Swagger UI serves as living documentation. `RoutePrefix = "swagger"` keeps the URL clean at `http://localhost:5000/swagger`.

---

## Prompt 6 — In-Memory Booking Store (No Database)

**Phase**: Implementation  
**Tool**: Claude

**Prompt**:
```
The spec says "must run fully offline on a local machine" and
"no database persistence" — but POST /cars/book must return a reference
and GET /cars/booking/{ref} must retrieve it.

How do I persist bookings for the lifetime of the app process
without any database, connection string, or setup step?
```

**Output Summary**:
AI recommended `ConcurrentDictionary<string, BookingResult>` as a thread-safe in-memory store injected as a `Singleton`. Key = reference number. `TryGetValue` for retrieval, dictionary indexer for save. Zero configuration — works immediately on `dotnet run`.

**Decision**:
Accepted over MySQL/SQLite/EF Core. Removes all infrastructure setup from the developer experience. Bookings reset on server restart — this is by design (spec says "no persistence"). The `BookingRepository` class is kept so it can be swapped for a real DB later with zero changes to `Program.cs`.

---

## Prompt 7 — Reference Number Format

**Phase**: Implementation  
**Tool**: Claude

**Prompt**:
```
Generate a booking reference number in format: SKY-{YYYYMMDD}-{4 random uppercase chars}
Requirements:
- Human-readable and memorable
- Based on pickup date (not booking date)
- Unique enough for a demo
- C# implementation using DateOnly
- Must be testable without instantiating BookingRepository
```

**Output Summary**:
```csharp
internal static string GenerateReference(DateOnly from)
{
    var datePart   = from.ToString("yyyyMMdd");
    var randomPart = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
    return $"SKY-{datePart}-{randomPart}";
}
```

**Decision**:
Accepted. Using the first 4 hex chars of a GUID is simple and collision-free for demo purposes. For production, a database sequence would be better. Exposed `GenerateReference` as `internal static` so the test project can call it directly without a database.

---

## Prompt 8 — CORS Configuration for file:// Frontend

**Phase**: Implementation  
**Tool**: Claude

**Prompt**:
```
The frontend is plain HTML/JS opened from the filesystem (file:// URL).
What CORS policy does a .NET 8 Minimal API need to allow requests from it?
My API runs on localhost:5000.

Options considered:
- AllowAnyOrigin()
- WithOrigins("null") for file:// protocol
- WithOrigins("http://localhost")
```

**Output Summary**:
AI flagged that `WithOrigins("null")` is unreliable — browsers differ on whether they send `"null"` or omit the Origin header entirely for `file://` origins. Recommended `AllowAnyOrigin()` for local-only offline demo scenarios.

**Decision**:
Used `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` registered before all routes. Appropriate only for local offline demo. Production would use `WithOrigins("https://app.skyroute.com")`.

---

## Prompt 9 — Unit Test Strategy (xUnit, AAA Pattern)

**Phase**: Testing  
**Tool**: Claude

**Prompt**:
```
I'm testing BudgetWheels weekend surcharge with xUnit.
Should I use a Theory with InlineData or separate [Fact] methods?

Also: BookingRepository is now an in-memory store.
How do I test GenerateReference without instantiating the full class?

Structure tests using Arrange-Act-Assert (AAA) per CLAUDE.md.
Focus on: BudgetWheels night-by-night 20% surcharge and edge-case date boundaries.
```

**Output Summary**:
AI recommended separate `[Fact]` methods for pricing — each with a manual calculation comment so the expected value is self-documenting. For `GenerateReference`, `internal static` + `InternalsVisibleTo` lets tests call it directly. 56 tests total across 4 files.

**Decision**:
Used `[Fact]` for pricing (clearer names: `BudgetWheels_FridayNight_IsConsideredWeekend`). Used `[Theory][InlineData]` for validation tests where the same rule applies across multiple cities. Made `CalculateTotal` and `CalculateTotalWithSurcharge` `internal static` for direct test access.

---

## Prompt 10 — Frontend State Management (No Framework)

**Phase**: Frontend  
**Tool**: Claude

**Prompt**:
```
I have 3 HTML pages: index.html (search), booking.html, confirmation.html.
Plain HTML/JS — no React, Vue, or Blazor.
Data needs to flow: selected vehicle → booking form → confirmation page.

Options:
1. sessionStorage
2. URL query parameters
3. localStorage

Which is best for this use case? Consider: data size, security, UX.
```

**Output Summary**:
AI recommended `sessionStorage`: cleared when tab closes (appropriate for a booking session), handles JSON objects cleanly, not visible in browser history, doesn't expose vehicle pricing in the URL bar.

**Decision**:
Accepted `sessionStorage`. `selectedVehicle` stored as JSON on "Book Now" click → read in `booking.js`. `bookingConfirmation` stored on successful POST → read in `confirmation.html` → cleared via "Search Again". Pickup/from/to passed via URL query params (small, non-sensitive).

---

## Prompt 11 — CSS Design System for Dark Mode UI

**Phase**: Frontend  
**Tool**: Claude

**Prompt**:
```
Create a CSS design system for a car rental web app.
Style: Premium, dark mode, modern.
Elements needed: header, search form card, vehicle cards with provider badge,
pricing display, booking form, confirmation card.
Use CSS custom properties (variables) for the design tokens.
Make it feel like a real travel platform, not a student project.
```

**Output Summary**:
AI produced CSS with dark palette (`--bg-dark: #0d0f1a`), gradient brand colors (`#6c63ff → #a78bfa`), card hover animations with `::before` top-border reveal, provider badges (gold for PremiumDrive, purple for BudgetWheels), and responsive CSS grid.

**Decision**:
Accepted the dark palette. Added spinning loader animation (`@keyframes spin`). Card hover uses `::before` pseudo-element for the gradient top border — more polished than a simple border-color change. Inter font from Google Fonts for premium typography.

---

## Prompt 12 — Vehicle Catalogue Expansion & Indian City Support

**Phase**: Enhancement  
**Tool**: Claude

**Prompt**:
```
Change domestic cities from London/Manchester to Bangalore, Mumbai, Delhi.
Expand both provider catalogues so each supports:
- Economy, Compact, SUV, Minivan (unified enum — already done)
- 3 vehicles per category per provider (not just 1)
- Real Indian car model names (not generic placeholders)
- Tiered pricing within each category (entry/mid/premium)
- BudgetWheels: keep the unavailable-vehicle filtering behaviour
- Display VehicleName prominently on each card alongside category
```

**Output Summary**:
- `CityRegistry.DomesticCities` → `{Bangalore, Mumbai, Delhi}`
- `PremiumDriveProvider`: 12 vehicles — Maruti Swift, Hyundai i20, Tata Altroz, Honda City, Hyundai Verna, VW Virtus, Tata Nexon, Hyundai Creta, Mahindra XUV700, Toyota Innova, Kia Carnival, Mercedes V-Class
- `BudgetWheelsProvider`: 16 entries (12 available, 4 unavailable for spec filtering coverage) — WagonR, Tiago, Kwid, Dzire, Amaze, Aura, Brezza, Seltos, Kushaq, Ertiga, Rumion, Marazzo
- `VehicleResult` model: new `VehicleName` property added
- Frontend: card headline changed to `vehicleName` with category shown as a pill badge

**Decision**:
Accepted. Indian market cars make the demo more realistic for the target audience. Kept 1 unavailable vehicle per category in BudgetWheels (4 total) to preserve the spec-required filtering behaviour. Tests updated: `PremiumDrive_NoCategory_Returns12Vehicles`, `BudgetWheels_NoCategory_Returns12AvailableVehicles`, `Service_BothProviders_ResultsMerged_24Total`, `Service_WithCategoryFilter_Returns6Vehicles`.

---

## Key Judgement Calls (Non-AI Decisions)

| Decision | Rationale |
|----------|-----------|
| `ConcurrentDictionary` in-memory store over MySQL | Spec says "fully offline" — no DB setup required; runs on any machine with just `dotnet run` |
| `Swashbuckle.AspNetCore` over `Microsoft.AspNetCore.OpenApi` alone | Swashbuckle provides the full Swagger UI; OpenApi alone only generates JSON |
| `internal static` pricing & reference methods | Directly testable via `InternalsVisibleTo` — no mocks, no test doubles needed |
| Client-side + server-side document validation | Defense in depth per spec; client JS pre-empts round-trip; server enforces regardless |
| Pricing in each provider, not a shared service | Providers have incompatible models — a shared service would need provider-specific branches |
| `Task.WhenAll` for provider aggregation | Both providers called in parallel; failure of one doesn't block the other |
| `partial class Program` at bottom of Program.cs | Enables `WebApplicationFactory<Program>` integration tests in future without refactor |
| 3 vehicles per category per provider | Gives genuine choice within each segment; mirrors real rental platforms that offer entry/mid/premium tiers |
| `sessionStorage` not `localStorage` | Booking state should not persist across browser sessions — sessionStorage auto-clears |
| `[Fact]` over `[Theory]` for pricing tests | Self-documenting test names; each case has a manual expected-value comment for verifiability |

## Current Tech Stack (as implemented)

```
Backend  : .NET 8 Minimal API (C#) — no EF Core, no MySqlConnector, no DotNetEnv
Store    : ConcurrentDictionary<string, BookingResult> (in-memory, singleton)
Swagger  : Swashbuckle.AspNetCore 6.6.2 + Microsoft.AspNetCore.OpenApi 8.0.7
Tests    : xUnit — 56 tests, 4 files (Pricing, Validation, Search, Booking)
Frontend : Vanilla HTML/CSS/JS — 3 pages, sessionStorage for state
Cities   : Domestic: Bangalore, Mumbai, Delhi | International: Paris, Dubai, New York, Tokyo, Sydney
Vehicles : 24 available (12 PremiumDrive + 12 BudgetWheels), 4 BudgetWheels filtered as unavailable
```

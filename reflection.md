# reflection.md — What I Would Improve With More Time

---

## What Went Well

**1. Provider Abstraction** — `ICarRentalProvider` achieved its extensibility goal cleanly. Adding a third provider (e.g., `LuxuryFleetProvider`) requires only a new class + one DI line. Zero changes to `CarRentalService`, endpoints, or frontend.

**2. Pricing Correctness** — BudgetWheels surcharge is implemented day-by-day as specified. The temptation to use a shortcut (`countWeekendNights × 1.2 + countWeekdayNights`) was avoided.

**3. Deterministic Stubs** — same input always gives same output; tests are reproducible and predictable.

**4. Single Configuration Point** — `.env` is the only file to change. Verified this works from a clean clone.

**5. Dual-layer Document Validation** — client-side (JavaScript) pre-empts round trips; server-side (.NET) ensures correctness regardless of what the frontend does.

---

## What I Would Improve With More Time

### 1. Authentication
No auth = any user can guess reference numbers. Would add JWT tokens; bookings tied to authenticated user.

### 2. EF Core with Migrations
Current: raw ADO.NET. Better: EF Core with `dotnet ef migrations add Initial` — schema always in sync with models, type-safe queries.

### 3. Integration Tests
Current tests are unit-only. Would add `WebApplicationFactory<Program>` tests to exercise the full HTTP stack, including validation middleware and response shapes.

### 4. Real-time Availability
BudgetWheels availability is hardcoded. Production version would cross-reference the Bookings table — a vehicle already booked for overlapping dates would be marked unavailable.

### 5. Structured Logging
Default console logging replaced with Serilog + JSON output. Add request correlation IDs for debugging.

### 6. Provider Timeout & Circuit Breaker
If a provider takes >3 seconds, the whole search hangs. Would add per-provider timeout via `CancellationToken` and Polly circuit breaker — return partial results if one provider fails.

### 7. Configuration Startup Validation
Currently a missing `.env` value fails silently at first DB call. Would add `IOptions<T>` validation that fails fast at startup with a clear message.

### 8. Frontend Build Pipeline
Currently: static files opened via `file://`. For a real team: Vite + TypeScript. Same user experience, but with type safety, hot reload, and bundling.

---

## AI Tooling Reflection

### What Accelerated Development
- **Boilerplate**: Models, interface, service skeletons generated quickly
- **Edge case identification**: AI flagged the Sunday-night edge case for BudgetWheels surcharge unprompted
- **CORS analysis**: Correctly identified that `WithOrigins("null")` is unreliable for `file://` origins

### Where AI Required Correction
- **Over-engineering**: AI suggested a `PricingStrategyFactory` pattern — rejected; pricing belongs in each provider
- **Test collision**: AI suggested testing 100 reference number calls as "all unique" — collisions occur with 4-char hex suffixes; reduced to 10 calls
- **Wrong CORS advice**: First suggestion was `WithOrigins("null")` — corrected after testing

### Critical Reflection
The spec.md-before-code discipline was the most valuable process decision. Writing the contracts first forced clarity about the data flow before any implementation started. The AI was most useful as a thought partner for designing the interface and for generating test cases with manual calculations in comments (which I then verified).

---

## Production Readiness Gap

| Concern | Current | Production |
|---------|---------|-----------|
| Auth | None | JWT/OAuth2 |
| DB migrations | schema.sql | EF Core migrations |
| Secrets | .env file | Key Vault |
| Observability | Console logs | Serilog + Seq |
| Resilience | None | Polly retry/circuit breaker |
| Testing | Unit only | Unit + Integration + E2E |
| Frontend | Static HTML | Vite + TypeScript |
| Availability | Hardcoded | Real-time DB check |

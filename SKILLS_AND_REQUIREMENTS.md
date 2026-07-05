# SkyRoute Car Rental — Skills, Objectives & Requirements

> A single reference document listing the skills demonstrated, objectives to achieve,
> and all functional/non-functional requirements for the Car Rental Availability feature.

---

## Skills Demonstrated

### Software Engineering Skills

| Skill | Where Used |
|-------|-----------|
| **Interface-driven design** | `ICarRentalProvider` abstraction — add providers without changing core code |
| **Dependency Injection** | All providers and services registered in DI container |
| **Async/Await** | Provider calls, database operations |
| **Strategy Pattern** | Each provider implements its own pricing strategy |
| **Input validation** | 400/422 HTTP responses with clear messages |
| **Unit testing** | xUnit tests covering pricing, validation, and booking |
| **Clean code** | Separate models, services, repositories, providers |

### Domain Knowledge Skills

| Skill | Where Used |
|-------|-----------|
| **REST API design** | GET/POST endpoints, correct HTTP status codes |
| **Weekend surcharge calculation** | Day-by-day iteration (not multiplication) |
| **Document validation rules** | Domestic vs. international classification |
| **Data modelling** | Unified VehicleResult from two different providers |

### Delivery Skills

| Skill | Where Used |
|-------|-----------|
| **spec-first development** | spec.md committed before any code |
| **AI tooling usage** | prompts.md documents all significant prompts |
| **Honest self-assessment** | reflection.md critiques the implementation |
| **Newbie-friendly documentation** | PREREQUISITES.md, detailed README |

---

## Objectives

### Primary Objective
Build a working, offline Car Rental Availability feature for the SkyRoute platform, demonstrating clean architecture, correct business logic, and professional delivery.

### Secondary Objectives

1. **Design before code**: Commit `spec.md` before any implementation
2. **Extensibility**: Third provider addition requires zero changes to existing code
3. **Correctness**: Weekend surcharge must iterate day-by-day; document validation must be dual-layer
4. **Operability**: Run from a clean clone using only the README
5. **AI honesty**: Document AI prompts and critically reflect on where AI helped and where it needed correction

---

## Requirements

### Functional Requirements — Providers

#### PremiumDrive
- [x] Flat daily rate pricing: `TotalPrice = DailyRate × nights`
- [x] Comprehensive insurance, included in price
- [x] Free cancellation up to 48h before pickup
- [x] Always available (no filtering needed)
- [x] Supports: Economy, Compact, SUV, Minivan

#### BudgetWheels
- [x] Base daily rate with weekend surcharge (20% on Fri/Sat/Sun nights)
- [x] Surcharge calculated by iterating each rental night, NOT by multiplication
- [x] Basic insurance only
- [x] Non-refundable cancellation
- [x] May return `available: false` — these must be filtered out
- [x] Supports: Economy, Compact, SUV, Minivan

### Functional Requirements — API

#### GET /cars/search
- [x] Query parameters: `pickup`, `from`, `to` (required); `category` (optional)
- [x] Return 400 if `pickup` is missing
- [x] Return 400 if `from` is missing
- [x] Return 400 if `to` is missing
- [x] Return 400 if `to` is not after `from`
- [x] Query both providers in parallel
- [x] Apply BudgetWheels pricing correctly
- [x] Filter unavailable BudgetWheels vehicles
- [x] Normalise results to unified `VehicleResult` model
- [x] Support optional `category` filter
- [x] Return both per-day rate and total price

#### POST /cars/book
- [x] Validate document type server-side
- [x] Return 422 with clear message on document mismatch
- [x] Confirm booking, return reference number
- [x] Store booking in MySQL database

#### GET /cars/booking/{reference}
- [x] Return booking details by reference number
- [x] Return 404 if reference not found

### Functional Requirements — Document Validation

- [x] International pickup → Passport required
- [x] Domestic pickup → National ID accepted (Passport also valid)
- [x] At least 2 domestic cities defined: London, Manchester
- [x] At least 3 international cities defined: Paris, Dubai, New York, Tokyo, Sydney
- [x] Validated client-side (JavaScript) AND server-side (.NET)
- [x] Return 422 with `code`, `message`, and `field` on mismatch

### Functional Requirements — Frontend

- [x] Search form: pickup location, pickup date, return date, optional category
- [x] Results: provider badge, category, per-day rate, total price, cancellation policy, insurance indicator
- [x] Results sortable by total price
- [x] Booking form: driver name, document type, document number
- [x] Booking form: client-side document validation
- [x] Confirmation: reference number, provider, total price, cancellation policy
- [x] Handle all states: loading, results, empty results, validation error, booking error, confirmation

### Non-Functional Requirements

- [x] No real rental APIs or credentials
- [x] Runs fully offline on a local machine
- [x] Third provider can be added without changing core flow
- [x] Stubs are deterministic (same input → same output)
- [x] No secrets committed to repository
- [x] `spec.md` committed before implementation files
- [x] AI tooling usage documented in `prompts.md`
- [x] Honest AI reflection in `reflection.md`

### Tech Stack Requirements

- [x] Backend: .NET 8 Minimal API (C#)
- [x] Frontend: Plain HTML/JS (no frameworks)
- [x] Tests: xUnit
- [x] Database: MySQL (local)
- [x] Connection string: configurable via `.env` file

---

## City Reference Table

| City | Type | Required Document |
|------|------|------------------|
| London | Domestic | National ID or Passport |
| Manchester | Domestic | National ID or Passport |
| Paris | International | Passport only |
| Dubai | International | Passport only |
| New York | International | Passport only |
| Tokyo | International | Passport only |
| Sydney | International | Passport only |

---

## Vehicle Category Mapping

| Unified Enum | PremiumDrive Label | BudgetWheels Label |
|-------------|-------------------|--------------------|
| Economy | Economy | Economy |
| Compact | Compact | Compact |
| SUV | SUV | SUV |
| Minivan | Minivan | Minivan |

Both providers use the same category names. Mapping is case-insensitive.

---

## Provider Comparison Table

| Feature | PremiumDrive | BudgetWheels |
|---------|-------------|-------------|
| Pricing | Flat daily rate | Base rate + weekend surcharge |
| Insurance | Comprehensive | Basic only |
| Cancellation | Free (up to 48h) | Non-refundable |
| Availability | Always | May be unavailable |
| Vehicles | 4 (always available) | 8 (4 available, 4 filtered) |

---

## HTTP Status Code Reference

| Code | Meaning | When Used |
|------|---------|-----------|
| 200 | OK | Successful GET |
| 201 | Created | Successful booking |
| 400 | Bad Request | Missing/invalid parameters |
| 404 | Not Found | Booking reference not found |
| 422 | Unprocessable Entity | Document type mismatch |

---

## Submission Checklist

- [ ] Public GitHub repository (no zip files)
- [ ] README.md — setup and run steps
- [ ] spec.md — committed before implementation
- [ ] CarRental.Api/ — .NET 8 backend
- [ ] CarRental.Tests/ — xUnit tests
- [ ] skyroute-ui/ — HTML/JS frontend
- [ ] prompts.md — AI prompts and decisions
- [ ] reflection.md — improvement analysis
- [ ] PREREQUISITES.md — A-Z setup guide
- [ ] EXECUTION_PLAN.md — implementation plan
- [ ] database/schema.sql — MySQL schema
- [ ] .env.example — configuration template
- [ ] .gitignore — excludes .env, bin/, obj/
- [ ] Application runs end-to-end from clean clone

# 🚗 SkyRoute Car Rental Availability

> Full-stack car rental search & booking platform — .NET 8 Minimal API · Plain HTML/JS · xUnit · Swagger/OpenAPI UI. Runs fully locally/offline with no external infrastructure dependencies.

---

## ⚡ Quick Start

```bash
# 1. Clone
git clone https://github.com/AbhijitPatilAJ/Car-rental.git
cd Car-rental

# 2. Start Backend API
cd CarRental.Api
dotnet run

# 3. Open Swagger UI
# Go to: http://localhost:5000/swagger

# 4. Open Frontend in browser
# Double-click: skyroute-ui\index.html
```

---

## Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download/dotnet/8.0 |
| Git | Any | https://git-scm.com/download/win |
| Browser | Chrome/Edge/Firefox | (built-in) |

---

## Running the Application

### Terminal 1 — Backend API

```bash
cd CarRental.Api
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### Browser — Frontend

Open `skyroute-ui/index.html` by double-clicking it in File Explorer.

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed console logs
dotnet test CarRental.Tests/ --logger "console;verbosity=detailed"
```

---

## Swagger UI Documentation

Access the interactive API documentation at: **[http://localhost:5000/swagger](http://localhost:5000/swagger)**

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Health check & API details |
| GET | `/cars/search?pickup=&from=&to=&category=` | Search available vehicles across both stubs |
| POST | `/cars/book` | Confirm booking with document validation |
| GET | `/cars/booking/{reference}` | Retrieve booking details by reference |

### Error Codes

| Code | When |
|------|------|
| 400 | Missing required params or invalid dates |
| 404 | Booking reference not found |
| 422 | Document type mismatch (NationalId at international pickup) |

---

## Pickup Locations & Validation Rules

| City | Type | Documents Accepted |
|------|------|-------------------|
| Bangalore | 🇮🇳 Domestic | Passport, National ID |
| Mumbai | 🇮🇳 Domestic | Passport, National ID |
| Delhi | 🇮🇳 Domestic | Passport, National ID |
| Paris | ✈️ International | Passport only |
| Dubai | ✈️ International | Passport only |
| New York | ✈️ International | Passport only |
| Tokyo | ✈️ International | Passport only |
| Sydney | ✈️ International | Passport only |

---

## Project Structure

```
car-rental/
├── README.md
├── spec.md                 ← Data models & API contracts
├── PREREQUISITES.md        ← Offline setup guide
├── EXECUTION_PLAN.md       ← Implementation plan
├── prompts.md              ← AI prompts used & decisions
├── reflection.md           ← Improvement ideas & reflection
├── CarRental.Api/
│   ├── Program.cs          ← API routes, Swagger configuration, CORS
│   ├── Interfaces/
│   │   └── ICarRentalProvider.cs
│   ├── Models/             ← Domain models
│   ├── Providers/          ← PremiumDrive (Flat rate) & BudgetWheels (Surcharge)
│   ├── Services/           ← CarRentalService & DocumentValidationService
│   └── Data/               ← BookingRepository (In-Memory ConcurrentDictionary)
├── CarRental.Tests/
│   ├── PricingTests.cs     ← Weekend surcharge logic tests
│   ├── ValidationTests.cs  ← Pickup location & document validation tests
│   ├── SearchTests.cs      ← Multi-provider merging & filtering tests
│   └── BookingTests.cs     ← Unique reference & storage tests
└── skyroute-ui/
    ├── index.html          ← Search dashboard
    ├── booking.html        ← Booking checkout page (with currency toggle)
    ├── confirmation.html   ← Booking receipt page
    ├── css/styles.css      ← Modern dark mode styles with surcharge breakdown box
    └── js/
        ├── api.js          ← API client and currency formatters
        ├── search.js       ← Form logic & search card rendering
        └── booking.js      ← Checkout form & sidebar surcharge breakdown
```

---

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| `ICarRentalProvider` interface | Add a 3rd provider with zero changes to existing aggregator logic |
| BudgetWheels day-by-day surcharge | Per spec: "iterate each night, not multiply". Friday, Saturday, Sunday nights cost 20% more. |
| In-Memory Booking Storage | Runs fully offline with no SQL Server setup needed. State persists for the process lifetime. |
| Swagger/OpenAPI | Generates live documentation and testing capabilities directly on port 5000. |
| Dual Currency Handling | Stored in INR; displays dual price (₹ INR and $ USD) for international pickups, allowing the traveller to select their booking currency. |
| Client + server document validation | Defense in depth per spec. Client-side JS provides fast UX; server-side C# enforces validation rules. |
| `sessionStorage` for page state | State is isolated to the tab session and automatically cleared when tab closes. |

---

## Troubleshooting

**Port 5000 in use**: Ensure another instance of `CarRental.Api` is not running. Kill it using:
```powershell
Get-Process -Name "CarRental.Api" -ErrorAction SilentlyContinue | Stop-Process -Force
```

**Frontend "Failed to fetch"**: Ensure `dotnet run` is running in your terminal.

**Swagger is not loading**: Make sure the backend is active at `http://localhost:5000`.

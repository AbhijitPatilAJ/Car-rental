# 🚗 SkyRoute Car Rental Availability

> Full-stack car rental search & booking platform — .NET 8 Minimal API · Plain HTML/JS · xUnit · MySQL

---

## ⚡ Quick Start

```bash
# 1. Clone
git clone https://github.com/YOUR_USERNAME/car-rental.git
cd car-rental

# 2. Configure — edit .env with your MySQL password
copy .env.example .env
notepad .env

# 3. Create database
mysql -u root -p < database\schema.sql

# 4. Start backend
cd CarRental.Api
dotnet run

# 5. Open frontend in browser
# Double-click: skyroute-ui\index.html
```

---

## Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download/dotnet/8.0 |
| MySQL Server | 8.0+ | https://dev.mysql.com/downloads/installer/ |
| Git | Any | https://git-scm.com/download/win |
| Browser | Chrome/Edge/Firefox | (built-in) |

> For a complete, step-by-step guide for first-time setup (including how to install each tool), see **[PREREQUISITES.md](./PREREQUISITES.md)**.

---

## Configuration

All configuration is in `.env` (copy from `.env.example`):

```dotenv
DB_HOST=localhost
DB_PORT=3306
DB_NAME=CarRentalDb
DB_USER=root
DB_PASSWORD=YOUR_MYSQL_PASSWORD_HERE
API_PORT=5000
```

**That is the only file you need to edit.**

---

## Running the Application

### Terminal 1 — Backend API

```bash
cd CarRental.Api
dotnet run
```

Expected output:
```
Now listening on: http://localhost:5000
```

### Browser — Frontend

Open `skyroute-ui/index.html` by double-clicking it in File Explorer.

### Running Tests

```bash
cd CarRental.Tests
dotnet test --verbosity normal
```

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Health check |
| GET | `/cars/search?pickup=&from=&to=&category=` | Search vehicles |
| POST | `/cars/book` | Confirm a booking |
| GET | `/cars/booking/{reference}` | Get booking details |

### Error Codes

| Code | When |
|------|------|
| 400 | Missing required param or invalid dates |
| 404 | Booking reference not found |
| 422 | Document type mismatch (NationalId at international pickup) |

---

## Pickup Locations

| City | Type | Documents Accepted |
|------|------|-------------------|
| London | 🇬🇧 Domestic | Passport, National ID |
| Manchester | 🇬🇧 Domestic | Passport, National ID |
| Paris | ✈️ International | Passport only |
| Dubai | ✈️ International | Passport only |
| New York | ✈️ International | Passport only |
| Tokyo | ✈️ International | Passport only |
| Sydney | ✈️ International | Passport only |

---

## Project Structure

```
car-rental/
├── .env                    ← Your config (NOT in git)
├── .env.example            ← Template
├── README.md
├── spec.md                 ← Data models & contracts
├── PREREQUISITES.md        ← A-Z setup guide
├── EXECUTION_PLAN.md       ← Implementation plan
├── prompts.md              ← AI prompts used
├── reflection.md           ← What to improve
├── database/
│   └── schema.sql          ← Run once to create DB
├── CarRental.Api/
│   ├── Program.cs          ← Routes + DI
│   ├── Interfaces/ICarRentalProvider.cs
│   ├── Models/             ← All domain models
│   ├── Providers/          ← PremiumDrive, BudgetWheels
│   ├── Services/           ← CarRentalService, DocumentValidation
│   └── Data/               ← BookingRepository (MySQL)
├── CarRental.Tests/
│   ├── PricingTests.cs     ← 11 tests
│   ├── ValidationTests.cs  ← 13 tests
│   ├── SearchTests.cs      ← 14 tests
│   └── BookingTests.cs     ← 7 tests
└── skyroute-ui/
    ├── index.html          ← Search page
    ├── booking.html        ← Booking form
    ├── confirmation.html   ← Booking confirmation
    ├── css/styles.css
    └── js/
        ├── api.js          ← Shared API client
        ├── search.js
        └── booking.js
```

---

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| `ICarRentalProvider` interface | Add a 3rd provider with zero changes to existing code |
| BudgetWheels day-by-day surcharge | Per spec: "iterate each night, not multiply" |
| `Task.WhenAll` for providers | Both providers called in parallel |
| MySqlConnector (raw ADO.NET) | Simple; no migrations; one schema.sql file |
| `.env` for connection string | One file to configure; git-ignored |
| Client + server document validation | Defense in depth per spec |
| `sessionStorage` for page state | Cleared on tab close; handles JSON objects |

---

## Troubleshooting

**MySQL connection failed**: Check DB_PASSWORD in `.env`; ensure MySQL service is running (Windows Services → MySQL80)

**Port 5000 in use**: Change `API_PORT` in `.env`; update `const API_BASE` in `skyroute-ui/js/api.js`

**dotnet command not found**: Install .NET 8 SDK; restart terminal

**Frontend "Failed to fetch"**: Ensure `dotnet run` is running in a separate terminal

**Database missing**: Run `mysql -u root -p < database\schema.sql`

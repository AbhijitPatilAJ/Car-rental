using System.Text.Json.Serialization;
using CarRental.Api.Data;
using CarRental.Api.Interfaces;
using CarRental.Api.Models;
using CarRental.Api.Providers;
using CarRental.Api.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── JSON — serialize enums as strings ────────────────────────────────────────
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// ── CORS — allows the HTML/JS frontend opened from file:// ───────────────────
builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ── Swagger / OpenAPI ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "SkyRoute Car Rental API",
        Version     = "v1",
        Description = "Search vehicles across PremiumDrive & BudgetWheels, book with document validation, retrieve booking details. Runs fully offline — no external services required.",
        Contact     = new OpenApiContact { Name = "SkyRoute Platform", Email = "dev@skyroute.example" }
    });
    // Ensure enums render as strings in Swagger UI
    c.UseInlineDefinitionsForEnums();
});

// ── Dependency Injection ─────────────────────────────────────────────────────
// Providers — register new providers here; no other code changes needed
builder.Services.AddSingleton<ICarRentalProvider, PremiumDriveProvider>();
builder.Services.AddSingleton<ICarRentalProvider, BudgetWheelsProvider>();

// Services
builder.Services.AddScoped<CarRentalService>();
builder.Services.AddScoped<DocumentValidationService>();

// In-memory booking store (singleton so bookings persist across requests)
builder.Services.AddSingleton<BookingRepository>();

// ── Build ────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseCors();

// ── Swagger UI (always on — runs offline, no auth needed) ────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyRoute Car Rental v1");
    c.RoutePrefix      = "swagger";
    c.DocumentTitle    = "SkyRoute Car Rental API";
    c.DefaultModelsExpandDepth(2);
    c.DisplayRequestDuration();
});

// ════════════════════════════════════════════════════════════════════════════
// ROUTE: GET /
// ════════════════════════════════════════════════════════════════════════════
app.MapGet("/", () => Results.Ok(new
{
    status  = "SkyRoute Car Rental API is running",
    version = "1.0",
    swagger = "http://localhost:5000/swagger"
}))
.WithName("HealthCheck")
.WithTags("Health")
.WithOpenApi(o =>
{
    o.Summary     = "Health check";
    o.Description = "Returns API status and Swagger UI URL.";
    return o;
})
.Produces<object>(200);

// ════════════════════════════════════════════════════════════════════════════
// ROUTE: GET /cars/search
// ════════════════════════════════════════════════════════════════════════════
app.MapGet("/cars/search", async (
    string? pickup,
    string? from,
    string? to,
    string? category,
    CarRentalService service) =>
{
    // ── Validate required parameters ──────────────────────────────────────
    if (string.IsNullOrWhiteSpace(pickup))
        return Results.BadRequest(new { error = "pickup is required" });

    if (string.IsNullOrWhiteSpace(from))
        return Results.BadRequest(new { error = "from date is required" });

    if (string.IsNullOrWhiteSpace(to))
        return Results.BadRequest(new { error = "to date is required" });

    if (!DateOnly.TryParse(from, out var fromDate))
        return Results.BadRequest(new { error = "from must be a valid date in format YYYY-MM-DD" });

    if (!DateOnly.TryParse(to, out var toDate))
        return Results.BadRequest(new { error = "to must be a valid date in format YYYY-MM-DD" });

    if (toDate <= fromDate)
        return Results.BadRequest(new { error = "to must be after from" });

    // ── Optional category filter ──────────────────────────────────────────
    VehicleCategory? vehicleCategory = null;
    if (!string.IsNullOrWhiteSpace(category))
    {
        if (!Enum.TryParse<VehicleCategory>(category, ignoreCase: true, out var parsedCat))
            return Results.BadRequest(new { error = "category must be one of: Economy, Compact, SUV, Minivan" });
        vehicleCategory = parsedCat;
    }

    var request = new SearchRequest { Pickup = pickup, From = fromDate, To = toDate, Category = vehicleCategory };
    var results = await service.SearchAsync(request);
    return Results.Ok(results);
})
.WithName("SearchVehicles")
.WithTags("Cars")
.WithOpenApi(o =>
{
    o.Summary     = "Search available vehicles";
    o.Description = """
        Queries **both** PremiumDrive and BudgetWheels in parallel.
        - Applies BudgetWheels night-by-night weekend surcharge (Fri/Sat/Sun + 20%).
        - Filters out BudgetWheels vehicles flagged as unavailable.
        - Returns a unified list sorted by provider then category.
        - `category` is optional — omit to return all categories.
        """;

    o.Parameters[0].Description = "Pickup city. One of: London, Manchester (domestic) or Paris, Dubai, New York, Tokyo, Sydney (international).";
    o.Parameters[1].Description = "Pickup date in YYYY-MM-DD format (e.g. 2025-08-04).";
    o.Parameters[2].Description = "Return date in YYYY-MM-DD format. Must be after `from`.";
    o.Parameters[3].Description = "Optional vehicle category filter: Economy | Compact | SUV | Minivan.";
    return o;
})
.Produces<IEnumerable<VehicleResult>>(200)
.ProducesProblem(400);

// ════════════════════════════════════════════════════════════════════════════
// ROUTE: POST /cars/book
// ════════════════════════════════════════════════════════════════════════════
app.MapPost("/cars/book", async (
    BookingRequest request,
    DocumentValidationService docValidator,
    BookingRepository repo) =>
{
    // ── Required field validation ─────────────────────────────────────────
    if (string.IsNullOrWhiteSpace(request.VehicleId))
        return Results.BadRequest(new { error = "vehicleId is required" });
    if (string.IsNullOrWhiteSpace(request.Provider))
        return Results.BadRequest(new { error = "provider is required" });
    if (string.IsNullOrWhiteSpace(request.Pickup))
        return Results.BadRequest(new { error = "pickup is required" });
    if (string.IsNullOrWhiteSpace(request.DriverName))
        return Results.BadRequest(new { error = "driverName is required" });
    if (string.IsNullOrWhiteSpace(request.DocumentNumber))
        return Results.BadRequest(new { error = "documentNumber is required" });
    if (request.To <= request.From)
        return Results.BadRequest(new { error = "to must be after from" });

    // ── Document validation (server-side — mirrors client-side JS logic) ──
    var validationError = docValidator.Validate(request.Pickup, request.DocumentType);
    if (validationError is not null)
        return Results.UnprocessableEntity(validationError);

    // ── Save in-memory and return confirmation ────────────────────────────
    var result = await repo.SaveAsync(request);
    return Results.Created($"/cars/booking/{result.ReferenceNumber}", result);
})
.WithName("BookVehicle")
.WithTags("Cars")
.WithOpenApi(o =>
{
    o.Summary     = "Book a vehicle";
    o.Description = """
        Validates the driver's document against the pickup location type:
        - **International** cities (Paris, Dubai, New York, Tokyo, Sydney) → **Passport required**.
        - **Domestic** cities (London, Manchester) → Passport or National ID accepted.
        
        On success, returns a booking reference in format `SKY-{YYYYMMDD}-{XXXX}`.
        All bookings are held in memory (no database) — data resets on server restart.
        """;
    return o;
})
.Accepts<BookingRequest>("application/json")
.Produces<BookingResult>(201)
.ProducesProblem(400)
.ProducesProblem(422);

// ════════════════════════════════════════════════════════════════════════════
// ROUTE: GET /cars/booking/{reference}
// ════════════════════════════════════════════════════════════════════════════
app.MapGet("/cars/booking/{reference}", async (string reference, BookingRepository repo) =>
{
    var booking = await repo.GetByReferenceAsync(reference);
    return booking is null
        ? Results.NotFound(new { error = $"Booking reference '{reference}' not found." })
        : Results.Ok(booking);
})
.WithName("GetBooking")
.WithTags("Cars")
.WithOpenApi(o =>
{
    o.Summary     = "Retrieve booking details";
    o.Description = "Returns full booking details for the given reference number (e.g. `SKY-20250804-A3F9`).";
    o.Parameters[0].Description = "Booking reference returned from POST /cars/book (format: SKY-YYYYMMDD-XXXX).";
    return o;
})
.Produces<BookingResult>(200)
.ProducesProblem(404);

app.Run();

// Expose for WebApplicationFactory integration tests
public partial class Program { }

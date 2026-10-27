using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UnifiedServiceScheduler.Api.Endpoints;
using UnifiedServiceScheduler.Api.Requests;
using UnifiedServiceScheduler.Api.Validation;
using UnifiedServiceScheduler.Application.Appointments;
using UnifiedServiceScheduler.Application.Common;
using UnifiedServiceScheduler.Domain.Repositories;
using UnifiedServiceScheduler.Domain.Services;
using UnifiedServiceScheduler.Infrastructure.Persistence;
using UnifiedServiceScheduler.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. Database & Persistence Layer
builder.Services.AddDbContext<UnifiedServiceSchedulerDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=unified_scheduler;Username=postgres;Password=postgres";

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly(typeof(UnifiedServiceSchedulerDbContext).Assembly.FullName);
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
    });
});

// 2. Unit of Work & Repositories
builder.Services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<ITechnicianRepository, TechnicianRepository>();
builder.Services.AddScoped<IServiceBayRepository, ServiceBayRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IDealershipRepository, DealershipRepository>();
builder.Services.AddScoped<IServiceTypeRepository, ServiceTypeRepository>();

// Specific repository classes for lock queries in coordinator
builder.Services.AddScoped<TechnicianRepository>();
builder.Services.AddScoped<ServiceBayRepository>();
builder.Services.AddScoped<AtomicSchedulingCoordinator>();

// 3. Domain Services
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();

// 4. Application Command Handlers
builder.Services.AddScoped<ICommandHandler<BookAppointmentCommand, Result<AppointmentResponseDto, SchedulingError>>, BookAppointmentCommandHandler>();

// 5. FluentValidation
builder.Services.AddScoped<IValidator<BookAppointmentRequest>, BookAppointmentRequestValidator>();

// 6. OpenAPI Documentation & CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors();

// Enable OpenAPI endpoint (/openapi/v1.json)
app.MapOpenApi();

// 7. Interactive API Explorer & Browser UI at Root ("/")
app.MapGet("/", () => Results.Content(@"<!doctype html>
<html lang=""en"">
  <head>
    <title>Unified Service Scheduler — Interactive API Explorer</title>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
    <link rel=""icon"" href=""data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 100 100%22><text y=%22.9em%22 font-size=%2290%22>🚗</text></svg>"">
    <style>
      body { margin: 0; padding: 0; background-color: #0f172a; }
    </style>
  </head>
  <body>
    <script id=""api-reference"" data-url=""/openapi/v1.json""></script>
    <script src=""https://cdn.jsdelivr.net/npm/@scalar/api-reference""></script>
  </body>
</html>", "text/html"))
.ExcludeFromDescription();

// Convenience redirects for /swagger and /docs to root UI
app.MapGet("/swagger", () => Results.Redirect("/")).ExcludeFromDescription();
app.MapGet("/docs", () => Results.Redirect("/")).ExcludeFromDescription();

// 8. Register Minimal API Endpoints
app.MapAppointmentEndpoints();
app.MapDevSeedEndpoints();

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }

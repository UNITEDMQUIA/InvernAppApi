using System.Text.Json.Serialization;
using InvernApp.Api.Data;
using InvernApp.Api.Middleware;
using InvernApp.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Base de datos (SQL Server). Falla al arrancar si falta la cadena de conexión.
var connectionString = builder.Configuration.GetConnectionString("InvernAppDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'InvernAppDb'. Defínela en appsettings.Development.json " +
        "o en la variable de entorno ConnectionStrings__InvernAppDb.");
}

builder.Services.AddDbContext<InvernAppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Servicios de la aplicación
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IMeasurementService, MeasurementService>();

// Controllers + JSON (los enums se serializan como texto, ej. "TURN_PUMP_ON")
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Documento OpenAPI nativo de .NET 10
builder.Services.AddOpenApi();

// Manejo global de errores
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "InvernApp API v1");
    });
}
else
{
    // En desarrollo el ESP32 usará HTTP en la red local; se revisa en la Fase 11.
    app.UseHttpsRedirection();
}

// Dashboard estático (wwwroot/index.html, Fase 10)
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Verificación rápida de que la API está viva
app.MapGet("/health", () => Results.Ok(new
{
    status = "OK",
    service = "InvernApp.Api",
    timestampUtc = DateTime.UtcNow
}));

app.Run();
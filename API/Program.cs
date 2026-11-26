using System.Text.Json.Serialization;
using InvernApp.Api.Data;
using InvernApp.Api.Middleware;
using InvernApp.Api.Security;
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

// API Key (cabecera X-API-Key). Falla al arrancar si falta o es demasiado corta.
var apiKey = builder.Configuration["Security:ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 16)
{
    throw new InvalidOperationException(
        "Falta la API Key 'Security:ApiKey' o tiene menos de 16 caracteres. En desarrollo use " +
        "'dotnet user-secrets set \"Security:ApiKey\" \"<clave>\"'; en producción, la variable de entorno Security__ApiKey.");
}

builder.Services.AddDbContext<InvernAppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Servicios de la aplicación
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IMeasurementService, MeasurementService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IAutomationService, AutomationService>();
builder.Services.AddScoped<ICommandService, CommandService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IActuatorService, ActuatorService>();

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

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "InvernApp API v1");

        // Swagger UI pide la API Key la primera vez que se hace una petición protegida,
        // la guarda en sessionStorage y la envía en la cabecera X-API-Key.
        options.UseRequestInterceptor("""
            (req) => {
                const needsKey = req.method !== 'GET' || /\/commands\/?(\?.*)?$/.test(req.url);
                if (needsKey) {
                    let key = sessionStorage.getItem('invernapp_api_key');
                    if (!key) {
                        key = window.prompt('API Key de InvernApp (cabecera X-API-Key):');
                        if (key) sessionStorage.setItem('invernapp_api_key', key.trim());
                    }
                    if (key) req.headers['X-API-Key'] = key.trim();
                }
                return req;
            }
            """);

        // Si el servidor responde 401, olvida la clave guardada para volver a pedirla.
        options.UseResponseInterceptor("""
            (res) => {
                if (res.status === 401) sessionStorage.removeItem('invernapp_api_key');
                return res;
            }
            """);
    });
}
else
{
    // En desarrollo el ESP32 usará HTTP en la red local; se revisa en la Fase 11.
    app.UseHttpsRedirection();
}

// Dashboard estático (wwwroot/index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

// API Key: protege las escrituras y los comandos del ESP32
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

// Verificación rápida de que la API está viva
app.MapGet("/health", () => Results.Ok(new
{
    status = "OK",
    service = "InvernApp.Api",
    timestampUtc = DateTime.UtcNow
}));

app.Run();
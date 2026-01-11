using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaymentGatewayService.Infrastructure;
using PaymentGatewayService.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar puerto
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{portEnv}");
}

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Configurar DbContext
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar HttpClient para BackendIntegrationService
builder.Services.AddHttpClient<IBackendIntegrationService, BackendIntegrationService>();

// Registrar servicios
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IBackendIntegrationService, BackendIntegrationService>();

// Configurar JWT Authentication
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"];
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "PaymentGatewayAPI";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "PaymentGatewayClients";

if (string.IsNullOrWhiteSpace(jwtSecretKey))
{
    throw new InvalidOperationException("JWT SecretKey no está configurada en appsettings.json");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configurar Swagger - configuración simple para evitar problemas de versiones
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Habilitar Swagger siempre (para debugging en producción también)
app.UseSwagger();
app.UseSwaggerUI();

// Middleware de manejo de excepciones global
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            var ex = error.Error;
            await context.Response.WriteAsJsonAsync(new { 
                mensaje = "Error interno del servidor", 
                detalle = ex.Message,
                tipo = ex.GetType().Name
            });
            Console.WriteLine($"[ERROR] {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    });
});

app.UseCors("AllowAll");

// Deshabilitar HTTPS redirection para Docker
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check mejorado
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    version = "1.0.0"
}));

Console.WriteLine($"[LISTO] Payment Gateway Service iniciando en: {string.Join(", ", app.Urls)}");
Console.WriteLine($"[INFO] Environment: {app.Environment.EnvironmentName}");

app.Run();


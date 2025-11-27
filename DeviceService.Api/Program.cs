using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

using DeviceService.Api.Auth;
using DeviceService.Api.Logging;
using DeviceService.Api.Middleware;
using DeviceService.Application;
using DeviceService.Application.Devices.Commands.UpdateDevice;
using DeviceService.Application.Interfaces;
using DeviceService.Application.Services;
using DeviceService.Infrastructure.Persistence;
using DeviceService.Infrastructure.Repositories;

using MediatR;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

using Serilog;
using CorrelationId;
using CorrelationId.DependencyInjection;

// =============
//  BUILDER
// =============
var builder = WebApplication.CreateBuilder(args);

// ========================================
//  LOAD ENVIRONMENT VARIABLES
// ========================================
builder.Configuration.AddEnvironmentVariables();

// ========================================
//  SERILOG CONFIGURATION (SIMPLIFIED)
// ========================================
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .Enrich.WithMachineName()
      .WriteTo.Console();
});

// ========================================
//  DB CONFIG (ENV VARS)
// ========================================
string GetEnv(string key, bool required = true)
{
    var value = Environment.GetEnvironmentVariable(key);

    if (required && string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"Missing required environment variable: {key}");

    return value ?? "";
}

var dbHost = GetEnv("DB_HOST");
var dbPort = GetEnv("DB_PORT");
var dbUser = GetEnv("DB_USERNAME");
var dbPass = GetEnv("DB_PASSWORD");
var dbName = GetEnv("DB_DATABASE");

var connectionString =
    $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};";

// ========================================
//  EF CORE (PostgreSQL + Retry)
// ========================================
builder.Services.AddDbContext<DeviceDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
});

// =======================================================
//  JWT CONFIGURATION (ENV VARS)
// =======================================================
var jwtIssuer = GetEnv("JWT_ISSUER");
var jwtAudience = GetEnv("JWT_AUDIENCE");
var jwtSecret = GetEnv("JWT_SECRET");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true
        };
    });

// Register TokenService for AuthController
builder.Services.AddSingleton<JwtTokenService>();

// ========================================
//  DEPENDENCY INJECTION (DOMAIN + APP)
// ========================================
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDevicesService, DevicesService>();
builder.Services.AddScoped<DevicesService>(); // optional: only if you inject concrete type

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(UpdateDeviceCommand).Assembly));

// ========================================
//  CONTROLLERS + JSON OPTIONS
// ========================================
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.WriteIndented = false;
    });

// =======================================================
//  SWAGGER / OPENAPI CONFIG
// =======================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Basic metadata
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartHome Device Service API",
        Version = "v1",
        Description = "API for managing SmartHome devices including registration, updates, retrieval and deletion."
    });

    // XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // JWT Bearer auth
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Annotations ([SwaggerOperation], [Tags])
    c.EnableAnnotations();
});

// =======================================================
//  HEALTH CHECKS
// =======================================================
builder.Services.AddHealthChecks()
    .AddNpgsql(connectionString, name: "postgres");

// =======================================================
//  OpenTelemetry: METRICS ONLY (Prometheus)
// =======================================================
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("SmartHome-DeviceService"))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter();
    });

// =======================================================
//  CORRELATION ID
// =======================================================
builder.Services.AddDefaultCorrelationId(options =>
{
    options.AddToLoggingScope = true;
    options.RequestHeader = "X-Correlation-ID";
    options.ResponseHeader = "X-Correlation-ID";
});

// =======================================================
//  RATE LIMITING
// =======================================================
builder.Services.AddRateLimiter(options =>
{
    // global rate limit: 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 100,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }
        )
    );

    // custom rejected response
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "text/plain";

        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please slow down",
            token
        );
    };
});

// =======================================================
//  RESPONSE CACHING
// =======================================================
builder.Services.AddResponseCaching();

// =======================================================
//  BUILD APP
// =======================================================
var app = builder.Build();

// =======================================================
//  GLOBAL MIDDLEWARE PIPELINE
// =======================================================

// Swagger (Dev only if you want; here enabled always)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartHome Device Service API v1");
});

// Early middleware
app.UseHttpsRedirection();

app.UseRateLimiter(); // rate limiting
app.UseResponseCaching();

// Custom correlation + logging middleware
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<LogEnrichmentMiddleware>();

// Route matching
app.UseRouting();

// Auth
app.UseAuthentication();
app.UseAuthorization();

// Custom request logging
app.UseCustomRequestLogging();

// Extra caching headers middleware
app.Use(async (context, next) =>
{
    context.Response.GetTypedHeaders().CacheControl =
        new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(30)
        };

    await next();
});

// Prometheus scraping endpoint (for metrics)
app.MapPrometheusScrapingEndpoint();

// Controllers
app.MapControllers();

// =======================================================
//  HEALTH CHECK ENDPOINTS
// =======================================================
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                exception = e.Value.Exception?.Message,
                duration = e.Value.Duration.ToString()
            })
        });

        await context.Response.WriteAsync(result);
    }
});

// =========================================
//  ENSURE DATABASE MIGRATIONS RUN WITH RETRY
// =========================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DeviceDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var maxRetries = 10;
    var delay = TimeSpan.FromSeconds(3);

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            logger.LogInformation("Attempt {Attempt}/{MaxRetries}: Applying migrations...", attempt, maxRetries);
            db.Database.Migrate();
            logger.LogInformation("Migrations applied successfully.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Migration attempt {Attempt} failed.", attempt);

            if (attempt == maxRetries)
            {
                logger.LogError("Max retry attempts reached. Migrations failed");
                throw;
            }

            await Task.Delay(delay);
        }
    }
}

app.Run();
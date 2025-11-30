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
using HealthChecks.NpgSql;
using DeviceService.Infrastructure.Seed;
using DeviceService.Api.Settings;
using DeviceService.Application.Cache;

// =============
//  BUILDER
// =============
var builder = WebApplication.CreateBuilder(args);

// =============================================
// CONFIGURATION SOURCES
// =============================================
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .AddIniFile(".env", optional: true)
    .AddIniFile(".env.development.local", optional: true)
    .AddIniFile(".env.docker", optional: true);

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
// ENV HELPER (GLOBAL SCOPE)
// ========================================
string GetEnv(string key, bool required = true)
{
    var value = Environment.GetEnvironmentVariable(key);

    if (required && string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"Missing required environment variable: {key}");

    return value ?? "";
}

// ========================================
//  DB CONFIG/CONNECTION
// ========================================
var dbSettings = builder.Configuration.GetSection("Database").Get<DatabaseSettings>()
    ?? throw new InvalidOperationException("Database configuration missing");

var connectionString = 
    $"Host={dbSettings.Host};" + 
    $"Port={dbSettings.Port};" + 
    $"Database={dbSettings.Database};" + 
    $"Username={dbSettings.Username};" + 
    $"Password={dbSettings.Password};"; 

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

//===============
// SEED DATA
//===============
builder.Services.AddScoped<SeedData>();

// ========================================
// JWT CONFIG 
// ========================================
string jwtIssuer;
string jwtAudience;
string jwtSecret;

if (builder.Environment.IsDevelopment())
{
    // MINIMUM 32 characters (256 bits)
    jwtIssuer = "local-dev-issuer";
    jwtAudience = "local-dev-audience";
    jwtSecret = "super-secret-local-dev-key-1234567890!!!";
}
else
{
    jwtIssuer = GetEnv("JWT_ISSUER");
    jwtAudience = GetEnv("JWT_AUDIENCE");
    jwtSecret = GetEnv("JWT_SECRET");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// =======================================================
//  JWT OPTIONS FOR TOKEN GENERATOR
// =======================================================
builder.Services.Configure<JwtOptions>(opts =>
{
    opts.Issuer = jwtIssuer;
    opts.Audience = jwtAudience;
    opts.Secret = jwtSecret;
    opts.ExpiryMinutes = 60;
});

builder.Services.AddSingleton<JwtTokenService>();


// ========================================
//  DEPENDENCY INJECTION (DOMAIN + APP)
// ========================================
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDevicesService, DevicesService>();
builder.Services.AddScoped<DevicesService>();


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

// ===========================================
//  HEALTH CHECKS
// ===========================================
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

// =======================================
//   REDIS CACHING
// =======================================
builder.Services.AddStackExchangeRedisCache(options => 
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "DeviceService";
});

builder.Services.AddSingleton<RedisCacheService>();


// ============================================
//  OpenTelemetry: METRICS ONLY (Prometheus)
// ============================================
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("SmartHome-DeviceService"))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            .AddMeter("System.Net.Http")
            .AddMeter("System.Net.NameResolution")
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

//=====================
// TRIGGER SEED DATA
//======================
var seedEnabled = Environment.GetEnvironmentVariable("SEED_ENABLED");

if (seedEnabled?.ToLower() == "true" )
{
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<SeedData>();
        await seeder.SeedAsync();
    }
}

app.Run();
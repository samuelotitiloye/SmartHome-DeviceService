using DeviceService.Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;   
using DeviceService.Application.Interfaces;
using DeviceService.Infrastructure.Repositories;
using DeviceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using DeviceService.Api.Auth;
using DeviceService.Application.Devices.Commands.UpdateDevice;
using DeviceService.Application.Services;
using Serilog;
using DeviceService.Api.Configuration;
using CorrelationId;
using CorrelationId.DependencyInjection;
using System.Text.Json;
using DeviceService.Api.Middleware;

// OpenTelemetry
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Extensions.Hosting;

// Prometheus.NET
using Prometheus;
//Rate limiting
using System.Threading.RateLimiting;



var builder = WebApplication.CreateBuilder(args);

// =======================================================
//  OpenTelemetry: Resource + Metrics + Traces
// =======================================================
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r =>
        r.AddService("SmartHome-DeviceService"))
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation();
        m.AddHttpClientInstrumentation();
        m.AddRuntimeInstrumentation();
        // NOTE: DO NOT call AddPrometheusExporter() here.
        // We are using prometheus-net instead of OTEL's exporter.
    })
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation(o => o.RecordException = true);
        t.AddHttpClientInstrumentation();
        t.AddSqlClientInstrumentation();
        t.AddOtlpExporter();
    });

// =======================================================
//   SERILOG 
// =======================================================
SerilogConfiguration.ConfigureLogging(builder);

// =======================================================
//   SERVICES
// =======================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// App Services
builder.Services.AddScoped<DevicesService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(UpdateDeviceCommand).Assembly));

// =======================================================
//   JWT CONFIGURATION
// =======================================================
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection.GetValue<string>("Secret") 
    ?? throw new InvalidOperationException("JWT secret is not configured in appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection.GetValue<string>("Issuer"),
            ValidAudience = jwtSection.GetValue<string>("Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// =======================================================
//   SWAGGER CONFIGURATION
// =======================================================
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Device Service API", 
        Version = "v1",
        Description = "API for managing smart home devices."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT token: Bearer {your token}", 
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
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    });
});

// =======================================================
//   DATABASE CONFIGURATION
// =======================================================
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

if (string.IsNullOrEmpty(dbUser) || string.IsNullOrEmpty(dbPassword))
{
    throw new InvalidOperationException("DB_USER or DB_PASSWORD environment variables are not set.");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("Database connection string 'DefaultConnection' is missing.");

connectionString = connectionString
    .Replace("${DB_USER}", dbUser)
    .Replace("${DB_PASSWORD}", dbPassword);

builder.Services.AddDbContext<DeviceDbContext>(options =>
    options.UseNpgsql(connectionString));

// =======================================================
//  HEALTH CHECKS
// =======================================================
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddCheck("database", new DbHealthCheck(connectionString), tags: new[] { "ready" });

// =======================================================
//  REPOSITORY
// =======================================================
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();

// =======================================================
//   CORRELATION ID
// =======================================================
builder.Services.AddDefaultCorrelationId(options =>
{
    options.AddToLoggingScope = true;
    options.RequestHeader = "X-Correlation-ID";
    options.ResponseHeader = "X-Correlation-ID";
});

// =======================================================
//   ADD RATE LIMITING
// =======================================================
builder.Services.AddRateLimiter(options =>
{
    //global rate limit: 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _=> new FixedWindowRateLimiterOptions
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
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please slow down",
            token
        );
    };
});

// =======================================================
//   BUILD APP
// =======================================================
var app = builder.Build();

// =======================================================
//   GLOBAL MIDDLEWARE PIPELINE 
// =======================================================
app.Use(async (context, next) =>
{
    var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();
    if (!string.IsNullOrEmpty(traceId))
    {
        // Use Append to avoid duplicate-key issues
        context.Response.Headers.Append("X-Trace-Id", traceId);
    }
    await next();
});

app.UseCorrelationId();
app.UseCustomRequestLogging();
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseRateLimiter();           //Rate Limiter

app.UseRouting();               // Required for Prometheus
app.UseHttpMetrics();           // Prometheus middleware (request metrics)

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

// =======================================================
//   PROMETHEUS /metrics ENDPOINT
// =======================================================
app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics();  // <- This exposes /metrics
});

// =======================================================
//   HEALTH ENDPOINTS
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
            checks = report.Entries.Select(e => new {
                name = e.Key,
                status = e.Value.Status.ToString(),
                exception = e.Value.Exception?.Message,
                duration = e.Value.Duration.ToString()
            })
        });

        await context.Response.WriteAsync(result);
    }
});

// =======================================================
//   ROUTING
// =======================================================
app.MapControllers();

app.Run();
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
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;

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
    // ==============================================================
    //  BASIC SWAGGER METADATA
    // ==============================================================
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartHome Device Service API",
        Version = "v1",
        Description = "API for managing SmartHome devices including registration, updates, retrieval and deletion."
    });

    // ==============================================================
    //  XML COMMENT LOADING (Api + Application + Domain)
    // ==============================================================
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // ==============================================================
    //  SECURITY: JWT BEARER AUTH
    // ==============================================================
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

    // ==============================================================
    //  OPERATION TAG SUPPORT (e.g., [Tags("Devices")])
    // ==============================================================
    c.EnableAnnotations(); // Required for [SwaggerOperation] & [Tags]
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
        context.HttpContext.Response.ContentType = "text/plain";  

        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please slow down",
            token
        );
    };
});

builder.Services.AddResponseCaching();


// =======================================================
//   BUILD APP
// =======================================================
var app = builder.Build();

// =======================================================
//   GLOBAL MIDDLEWARE PIPELINE 
// =======================================================

app.UseCorrelationId();
app.Use(async (context, next) =>
{
    await next();

    if (!context.Response.HasStarted)
    {
        var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrEmpty(traceId))
            context.Response.Headers["X-Trace-Id"] = traceId;
    }
});

app.UseCustomRequestLogging();  // response caching service
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseRateLimiter();           //Rate Limiter

app.UseRouting();               // Required for Prometheus

// add caching middleware (headers + middleware)
app.UseResponseCaching();       
app.Use(async (context, next) =>
{
    context.Response.GetTypedHeaders().CacheControl =
        new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(30) //lifetime
        };
    await next();
});

// Prometheus middleware (request metrics)
app.UseHttpMetrics();           

app.UseAuthentication();
app.UseAuthorization();

// =======================================================
//   SWAGGER (Enable before MapControllers())
// =======================================================
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/swagger"))
    {
        ctx.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        ctx.Response.Headers["Pragma"] = "no-cache";
        ctx.Response.Headers["Expires"] = "0";
    }
    await next();
});

app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartHome Device Service API v1");
});

// =======================================================
//   PROMETHEUS /metrics
// =======================================================
app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics();  // exposes /metrics
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
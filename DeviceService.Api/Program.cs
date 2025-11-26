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
using DeviceService.Application;
using System.Diagnostics;
using Serilog.Enrichers.Span;
using DeviceService.Api.Middleware;
using Serilog.Events;
using Serilog.Formatting;
using DeviceService.Api.Logging;
using OpenTelemetry.Exporter;



var builder = WebApplication.CreateBuilder(args);

// Load external Serilog config
builder.Configuration.AddJsonFile("serilog.json", optional: false, reloadOnChange: true);

// =======================================================
//  OpenTelemetry: Resource + Metrics + Traces
// =======================================================
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("SmartHome-DeviceService"))
        .WithTracing(tracing => 
        {
            tracing
                // incoming ASP.NET reqs
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;

                    // eclude health endpoints
                    options.Filter = ctx =>
                    {
                        var path = ctx.Request.Path.Value;
                        if (string.IsNullOrEmpty(path))
                            return true;
                        
                        return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase);
                    };

                })
                // outgoing HttpClient calls
                .AddHttpClientInstrumentation()
                //Exporter -> Jaeger
                .AddJaegerExporter(jaeger =>
                {
                    jaeger.AgentHost = "localhost";
                    jaeger.AgentPort = 6831;
                });
        })
        .WithMetrics(metrics =>
        {
            metrics
            // ASP.NET metrics
            .AddAspNetCoreInstrumentation()

            // HttpClient metrics
            .AddHttpClientInstrumentation()

            // .NET runtime metrics
            .AddRuntimeInstrumentation()

            // CPU, memory, thread-pool metrics
            .AddProcessInstrumentation()

            // EXPORTER → Prometheus
            .AddPrometheusExporter();
        });


// =======================================================
//   SERILOG 
// =======================================================
builder.Host.UseSerilog((context, services, configuration) => 
{
    configuration
         // -------------------------------
        // 1. Load from serilog.json first
        // --------------------------------
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)

        // -------------------------------
        // 2. Core Log Sinks
        // -------------------------------
        .WriteTo.Console(new PrettyJsonFormatter())
        .WriteTo.File(new PrettyJsonFormatter(), "logs/log-.json", rollingInterval: RollingInterval.Day)
        
        // --------------------------------
        // 3. Add ENRICHERS 
        // --------------------------------
        .Enrich.FromLogContext()
        .Enrich.WithSpan()
        .Enrich.WithCorrelationId()
        .Enrich.WithMachineName()
        .Enrich.WithProcessId()
        .Enrich.WithProcessName()
        .Enrich.WithThreadId()
        .Enrich.WithEnvironmentUserName()
        .Enrich.WithProperty("ServiceName", "DeviceService.Api")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);
});

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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
   ?? throw new Exception("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<DeviceDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => 
    {
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    }));

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
//----------Early Middleware(pre routing)
app.UseHttpsRedirection();
app.UseRateLimiter();           //Rate Limiter
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<LogEnrichmentMiddleware>();
app.UseResponseCaching();  
app.UseHttpMetrics();    

//-------Routing-------
app.UseRouting();               
if (!app.Environment.IsDevelopment() && Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    app.UseHttpsRedirection();
}

//-------Auth/Auth-----
app.UseAuthentication();
app.UseAuthorization();

// ----------------------------------------------
//      CUSTOM REQUEST LOGGING
// ----------------------------------------------
app.UseCustomRequestLogging(); 

// ----------------------------------------------
// Prometheus scraping endpoint
// ----------------------------------------------
app.MapPrometheusScrapingEndpoint();

// add caching middleware (headers + middleware)
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

// =======================================================
//   SWAGGER (static assets)
// =======================================================
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartHome Device Service API v1");
});

//----- endpoint route selection starts MVC-----
app.MapControllers();

// =======================================================
//   HEALTH CHECKS
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

// =========================================
// ENSURE DATABASE MIGRATIONS RUN WITH RETRY
// ==========================================
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
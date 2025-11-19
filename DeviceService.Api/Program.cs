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

var builder = WebApplication.CreateBuilder(args);

// ==================================
//   SERILOG 
// ==================================
SerilogConfiguration.ConfigureLogging(builder);

// ==================================
//   SERVICES
// ==================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ==================================
//   APP SERVICES
// ==================================
builder.Services.AddScoped<DevicesService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(UpdateDeviceCommand).Assembly));

// ==================================
//   JWT CONFIGURATION
// ==================================
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

// ==================================
//   SWAGGER CONFIGURATION
// ==================================
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

// ======================================
//   DATABASE INTEGRATION CONFIGURATION
// ======================================
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("Database connection string 'DefaultConnection' is missing.");

connectionString = connectionString
    .Replace("${DB_USER}", dbUser)
    .Replace("${DB_PASSWORD}", dbPassword);

builder.Services.AddDbContext<DeviceDbContext>(options =>
    options.UseNpgsql(connectionString));

// ==================================
//  HEALTH CHECKS
// ==================================
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddCheck("database", new DbHealthCheck(connectionString), tags: new[] { "ready" });

// ==================================
//  REGISTER REPOSITORY
// ==================================
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();

// ==================================
//   CORRELATION ID
// ==================================
builder.Services.AddDefaultCorrelationId(options =>
{
    options.AddToLoggingScope = true;
    options.RequestHeader = "X-Correlation-ID";
    options.ResponseHeader = "X-Correlation-ID";
});

// ==================================
//   BUILD APP
// ==================================
var app = builder.Build();

// ==================================
//   GLOBAL MIDDLEWARE PIPELINE 
// ==================================

app.UseCorrelationId();                 // Correlation ID

app.UseCustomRequestLogging();          // custom request logging(middleware)
app.UseSerilogRequestLogging();         // Request Logging

app.UseHttpsRedirection();              // HTTPS redirect
app.UseSwagger();                       // Swagger UI
app.UseSwaggerUI();

app.UseAuthentication();                // Auth
app.UseAuthorization();                 // Authorization

// ==================================
//   HEALTH ENDPOINTS
// ==================================
app.MapHealthChecks("/health"); //basic health - always returns healthy: for uptime monitors(loadbalancer in cloud)
app.MapHealthChecks("/health/live", new HealthCheckOptions // checks app is running
{
    Predicate = _ => false // only self
});

app.MapHealthChecks("health/ready", new HealthCheckOptions // checks db availability. returns structured JSON
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


// ==================================
//   ROUTING
// ==================================
app.MapControllers();                   // Routing

app.Run();

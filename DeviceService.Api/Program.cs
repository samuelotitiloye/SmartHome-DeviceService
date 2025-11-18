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
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<DevicesService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(UpdateDeviceCommand).Assembly));

// Load JWT options from configuration
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();


// ==================================
//   JWT CONFIGURATION
// ==================================
// Add JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection.GetValue<string>("Secret") 
    ?? throw new InvalidOperationException("JWT secret is not configured in appsettings.json");

Console.WriteLine("JWT CONFIG DEBUG:");
Console.WriteLine($"Issuer: {jwtSection.GetValue<string>("Issuer")}");
Console.WriteLine($"Audience: {jwtSection.GetValue<string>("Audience")}");
Console.WriteLine($"Secret: {jwtSection.GetValue<string>("Secret")}");    

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
// Swagger
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

// Load envirenment variables for DB connection
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

// Load Base connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("Database connection string 'DefaultConnection' is missing.");
    
// Inject environment variables into the placeholders   
connectionString = connectionString 
    .Replace("${DB_USER}", dbUser)
    .Replace("${DB_PASSWORD}", dbPassword);

    Console.WriteLine($"DB_USER loaded: {dbUser}");

Console.WriteLine("DB CONNECTION STRING DEBUG:" + connectionString);

// Register DbContext ONCE
builder.Services.AddDbContext<DeviceDbContext>(options =>
    options.UseNpgsql(connectionString));


// Register Repository 
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();


var app = builder.Build();

// ==================================
//   GLOBAL MIDDLEWARE PIPELINE
// ==================================

// Log incoming/outgoing HTTP
app.UseHttpsRedirection();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Device Service API v1");
});

// Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Routing
app.MapControllers();

app.Run();

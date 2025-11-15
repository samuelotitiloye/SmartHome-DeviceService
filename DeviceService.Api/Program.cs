using Microsoft.OpenApi.Models;
using DeviceService.Application.Services;
using DeviceService.Domain.Repositories;
using DeviceService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<DevicesService>();
builder.Services.AddSingleton<IDeviceRepository, InMemoryDeviceRepository>();


builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.SwaggerDoc("v1", new OpenApiInfo 
    { Title = "Device Service API", 
    Version = "v1",
    Description = "API for managing smart home devices."
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Device Service API v1");
});

app.MapControllers();
app.Run();

using Serilog;

namespace DeviceService.Api.Configuration
{
    public static class SerilogConfiguration
    {
        public static void ConfigureLogging(WebApplicationBuilder builder)
        {
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessId()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/deviceservice-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true
                )
                .CreateLogger();

                builder.Logging.ClearProviders();
                builder.Host.UseSerilog(logger, dispose: true);
        }
    }
}
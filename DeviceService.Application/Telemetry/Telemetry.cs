using System.Diagnostics;

namespace DeviceService.Application
{
    public static class Telemetry
    {
        public const string ActivitySourceName = "DeviceService";
        public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    }
}

using System.Diagnostics;

namespace DeviceService.Api.Telemetry
{
    internal static class Telemetry
    {
        public const string ActivitySourceName = "DeviceService";
        public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    }
}

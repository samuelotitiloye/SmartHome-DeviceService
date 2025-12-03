namespace DeviceService.Infrastructure.Configuration
{
    public class RedisSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 6379;
        public string? Password  { get; set; }
        public string InstanceName  { get; set; } = "deviceservice:";
    }
}
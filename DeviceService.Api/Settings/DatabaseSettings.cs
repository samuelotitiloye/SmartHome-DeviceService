namespace DeviceService.Api.Settings
{
    public class DatabaseSettings
    {
        public string Host { get; set; } = "";
        public string Port { get; set; } = "5432";
        public string Database { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}

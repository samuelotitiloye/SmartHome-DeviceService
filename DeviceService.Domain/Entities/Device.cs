namespace DeviceService.Domain.Entities
{
    public class Device
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string DeviceName { get; private set; }
        public string Type { get; private set; }
        public string Location { get; private set; }
        public int ThresholdWatts { get; private set; }
        public string SerialNumber { get; private set; }
        public DateTime RegisteredAt { get; private set; } = DateTime.UtcNow;

        public Device(string deviceName, string type, string location, int thresholdWatts, string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
                throw new ArgumentException("Device name is required.");

            DeviceName = deviceName;
            Type = type;
            Location = location;
            ThresholdWatts = thresholdWatts;
            SerialNumber = serialNumber;
            RegisteredAt = DateTime.UtcNow;
        }
    }
}
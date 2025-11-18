using System;

namespace DeviceService.Domain.Entities
{
    public class Device
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public int ThresholdWatts { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }

        //EF Core requires this for materialization
        public Device() { }

        // real constructor used by code/tests
        public Device(
            Guid id,
            string name,
            string type,
            string location,
            bool isOnline,
            int thresholdWatts,
            string serialNumber,
            DateTime registeredAt
        )
        {
            Id = id;
            Name = name;
            Type = type;
            Location = location;
            IsOnline = isOnline;
            ThresholdWatts = thresholdWatts;
            SerialNumber = serialNumber;
            RegisteredAt = registeredAt;
        }
    }
}

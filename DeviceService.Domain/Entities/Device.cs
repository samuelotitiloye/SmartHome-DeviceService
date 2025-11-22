using System;

namespace DeviceService.Domain.Entities
{
    /// <summary>
    /// Represents a SmartHome device stored in the system database.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// Unique identifier for the device.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Human-readable name assigned to the device.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Category or type of the device (e.g., Thermostat, Sensor, Camera).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Physical installation location of the device.
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the device is online and actively reporting.
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// Configured wattage threshold for energy monitoring.
        /// </summary>
        public int ThresholdWatts { get; set; }

        /// <summary>
        /// Manufacturer-assigned serial number for the device.
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp indicating when the device was registered in the system.
        /// </summary>
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Required by Entity Framework Core for entity materialization.
        /// </summary>
        public Device() { }

        /// <summary>
        /// Creates a fully initialized SmartHome device entity.
        /// </summary>
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

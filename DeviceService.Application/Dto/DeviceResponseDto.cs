
namespace DeviceService.Application.Dto
{
    /// <summary>
    /// Device response payload returned by the API.
    /// </summary>
    public class DeviceResponseDto
    {
        /// <summary>Unique identifier for the device.</summary>
        public Guid Id { get; set; }

        /// <summary>Human-friendly name of the device.</summary>
        public string DeviceName { get; set; }

        /// <summary>The category/type of the device (e.g., Light, Fan).</summary>
        public string Type { get; set; }

        /// <summary>Where the device is located.</summary>
        public string Location { get; set; }

        /// <summary>Upper limit for the device's power usage in watts.</summary>
        public int ThresholdWatts { get; set; }

        /// <summary>Manufacturer serial number.</summary>
        public string SerialNumber { get; set; }

        /// <summary>When the device was registered in the system (UTC).</summary>
        public DateTime RegisteredAt { get; set; }
    }
}

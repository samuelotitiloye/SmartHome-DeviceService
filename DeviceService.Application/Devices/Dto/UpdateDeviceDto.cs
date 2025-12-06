namespace DeviceService.Application.Devices.Dto
{
    /// <summary>
    /// Represents updated values for an existing SmartHome device.
    /// </summary>
    public class UpdateDeviceDto
    {
        /// <summary>The updated name of the device.</summary>
        public required string Name { get; set; }

        /// <summary>The updated device type.</summary>
        public required string Type { get; set; }

        /// <summary>The updated physical location of the device.</summary>
        public required string Location { get; set; }

        /// <summary>Indicates whether the device is currently online.</summary>
        public bool IsOnline { get; set; }

        /// <summary>The new energy threshold limit (watts).</summary>
        public int? ThresholdWatts { get; set; }

        /// <summary>The updated serial number (optional).</summary>
        public string? SerialNumber { get; set; }
    }
}

namespace DeviceService.Application.Devices.Dto
{
    /// <summary>
    /// Represents a SmartHome device returned in API responses.
    /// </summary>
    /// <param name="Id">Unique device identifier.</param>
    /// <param name="Name">Device name.</param>
    /// <param name="Type">Device type (e.g., Thermostat, Camera, Sensor).</param>
    /// <param name="Location">Physical location of the device.</param>
    /// <param name="IsOnline">Indicates whether the device is currently online.</param>
    /// <param name="ThresholdWatts">Configured power threshold (watts), if provided.</param>
    /// <param name="SerialNumber">Manufacturer serial number, if available.</param>
    /// <param name="RegisteredAt">Timestamp of when the device was registered.</param>
    public record DeviceDto(
        Guid Id,
        string Name,
        string Type,
        string Location,
        bool IsOnline,
        int? ThresholdWatts,
        string? SerialNumber,
        DateTime RegisteredAt
    );
}

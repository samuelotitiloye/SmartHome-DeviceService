namespace DeviceService.Application.Devices.Dto
{
    /// <summary>
    /// Represents the data required to register a new SmartHome device.
    /// </summary>
    /// <param name="Name">The name assigned to the device.</param>
    /// <param name="Type">The device type (e.g., Thermostat, Sensor, Switch).</param>
    /// <param name="Location">The physical installation location of the device.</param>
    /// <param name="IsOnline">Indicates whether the device is currently online.</param>
    /// <param name="ThresholdWatts">Optional power usage threshold for alerting (watts).</param>
    /// <param name="SerialNumber">Optional manufacturer-provided serial number.</param>
    public record RegisterDeviceDto(
    string Name,
    string Type,
    string Location,
    bool IsOnline,
    int? ThresholdWatts,
    string? SerialNumber
    );
}

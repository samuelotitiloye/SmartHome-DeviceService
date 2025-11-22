/// <summary>
/// Represents updated values for an existing SmartHome device.
/// </summary>
public class UpdateDeviceDto
{
    /// <summary>The updated name of the device.</summary>
    public string Name { get; set; }

    /// <summary>The updated device type.</summary>
    public string Type { get; set; }

    /// <summary>The updated physical location of the device.</summary>
    public string Location { get; set; }

    /// <summary>Indicates whether the device is currently online.</summary>
    public bool IsOnline { get; set; }

    /// <summary>The new energy threshold limit (watts).</summary>
    public int? ThresholdWatts { get; set; }

    /// <summary>The updated serial number.</summary>
    public string? SerialNumber { get; set; }
}

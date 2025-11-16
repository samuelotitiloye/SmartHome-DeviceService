namespace DeviceService.Api.Contracts;

public class UpdateDeviceRequest
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Location { get; set; } = default!;
    public bool IsOnline { get; set; }
    public int ThresholdWatts { get; set; }
    public string SerialNumber { get; set; } = default!;
}
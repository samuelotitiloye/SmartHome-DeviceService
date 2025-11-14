namespace DeviceService.Application.Dto
{
    public record RegisterDeviceDto(
        string DeviceName,
        string Type,
        string Location,
        int ThresholdWatts,
        DateTime RegisteredAt
    );
}
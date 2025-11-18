namespace DeviceService.Application.Dto
{
    public record RegisterDeviceDto(
        string Name,
        string Type,
        string Location,
        bool IsOnline,
        int ThresholdWatts,
        string SerialNumber
    );
}

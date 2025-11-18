namespace DeviceService.Application.Devices.Dto
{
    public record DeviceDto(
        Guid Id,
        string Name,
        string Type,
        string Location,
        bool IsOnline,
        int ThresholdWatts,
        string SerialNumber,
        DateTime RegisteredAt
    );
}

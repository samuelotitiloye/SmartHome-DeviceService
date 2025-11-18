namespace DeviceService.Application.Dto
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

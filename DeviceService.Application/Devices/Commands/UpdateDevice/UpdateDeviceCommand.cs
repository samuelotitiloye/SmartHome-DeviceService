using DeviceService.Application.Devices.Dto;
using MediatR;

namespace DeviceService.Application.Devices.Commands.UpdateDevice
{
    /// <summary>
    /// Command to update an existing SmartHome device.
    /// Returns the updated <see cref="DeviceDto"/> or null if not found.
    /// </summary>
    public sealed record UpdateDeviceCommand(
        Guid Id,
        string Name,
        string Type,
        string Location,
        bool IsOnline,
        int? ThresholdWatts,
        string? SerialNumber
    ) : IRequest<DeviceDto?>;
}

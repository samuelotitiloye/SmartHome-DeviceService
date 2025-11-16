using MediatR;
using DeviceService.Application.Dto;

namespace DeviceService.Application.Devices.Commands.UpdateDevice
{
    public record UpdateDeviceCommand(
        Guid Id,
        string Name,
        string Type,
        string Location,
        bool IsOnline,
        int ThresholdWatts,
        string SerialNumber
    ) : IRequest<DeviceDto?>;
}

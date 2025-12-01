using DeviceService.Application.Devices.Dto;
using MediatR;

namespace DeviceService.Application.Devices.Commands.RegisterDevice
{
    public record RegisterDeviceCommand(
        string Name,
        string Type,
        string Location,
        bool IsOnline,
        int ThresholdWatts,
        string SerialNumber
    ) : IRequest<DeviceDto>;
}

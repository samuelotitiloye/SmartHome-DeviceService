using MediatR;

namespace DeviceService.Application.Devices.Commands.DeleteDevice
{
    public record DeleteDeviceCommand(Guid Id) : IRequest<bool>;
}

using DeviceService.Application.Devices.Dto;
using MediatR;

namespace DeviceService.Application.Devices.Queries.GetDeviceById
{
    public record GetDeviceByIdQuery(Guid Id) : IRequest<DeviceDto?>;
}

using DeviceService.Application.Common.Models;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Devices.Models;
using MediatR;

namespace DeviceService.Application.Devices.Queries.ListDevices
{
    public record ListDevicesQuery(
        DeviceFilter Filter,
        PaginationParameters Pagination
    ) : IRequest<PaginatedResult<DeviceDto>>;
}

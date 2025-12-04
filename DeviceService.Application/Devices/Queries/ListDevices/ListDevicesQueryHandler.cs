using DeviceService.Application.Common.Models;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Devices.Models;
using DeviceService.Application.Interfaces;
using DeviceService.Application.Mappings;
using MediatR;

namespace DeviceService.Application.Devices.Queries.ListDevices
{
    public class ListDevicesQueryHandler : IRequestHandler<ListDevicesQuery, PaginatedResult<DeviceDto>>
    {
        private readonly IDeviceRepository _repo;

        public ListDevicesQueryHandler(IDeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task<PaginatedResult<DeviceDto>> Handle(
            ListDevicesQuery request, CancellationToken ct)
        {
            var result = await _repo.GetDevicesAsync(
                request.Filter,
                request.Pagination,
                ct
            );

            var dtoItems = result.Items.Select(d => d.ToDto()).ToList();

            return new PaginatedResult<DeviceDto>(
                dtoItems,
                result.TotalCount,
                result.PageNumber,
                result.PageSize
            );
        }
    }
}

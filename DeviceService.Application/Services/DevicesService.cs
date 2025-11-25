using System.Diagnostics;
using DeviceService.Application.Devices.Dto; 
using DeviceService.Application.Mappings; 
using DeviceService.Application.Interfaces; 
using DeviceService.Application.Devices.Queries; 
using DeviceService.Application.Interfaces; 
using DeviceService.Application.Common.Models; 
using DeviceService.Application.Devices.Models; 
using DeviceService.Domain.Entities; 

namespace DeviceService.Application.Services
{
    public class DevicesService
    {
        private readonly IDeviceRepository _repo;

        public DevicesService(IDeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task<DeviceDto> RegisterDeviceAsync(RegisterDeviceDto dto, CancellationToken ct)
        {
            var device = dto.ToEntity();

            await _repo.AddAsync(device);

            return device.ToDto();
        }

        public Task<DeviceDto?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return _repo.GetByIdAsync(id)
                .ContinueWith(t => t.Result?.ToDto(), ct);
        }

        // public async Task<IEnumerable<DeviceDto>> GetAllAsync(CancellationToken ct)
        // {
        //     var devices = await _repo.GetAllAsync();
        //     return devices.Select(d => d.ToDto());
        // }

        public async Task<PaginatedResult<Device>> GetPagedAsync(
            int page,
            int pageSize,
            string? type,
            string? location,
            bool? isOnline,
            CancellationToken cancellationToken = default)
        {
            var pagination = new PaginationParameters(page, pageSize);

            var filter = new DeviceFilter
            {
                NameContains = null,
                Location = location,
                Type = type,
                IsOnline = isOnline,
                MinThresholdWatts = null,  // adjust if needed later
                SortBy = DeviceSortBy.RegisteredAt,
                SortOrder = SortOrder.Desc
            };

            return await _repo.GetDevicesAsync(filter, pagination, cancellationToken);
        }

        public async Task<PaginatedResult<DeviceDto>> GetDevicesAsync(DeviceFilter filter, PaginationParameters pagination, CancellationToken cancellationToken = default)
        {
            using var activity = Telemetry.ActivitySource.StartActivity("DevicesService.GetDevice");

            activity?.SetTag("filter.Type", filter.Type ?? "null");
            activity?.SetTag("filter.Location", filter.Location ?? "null");
            activity?.SetTag("filter.isOnline", filter.IsOnline?.ToString() ?? "null");
            activity?.SetTag("pagination.pageNumber", pagination.PageNumber);
            activity?.SetTag("pagination.pageSize", pagination.PageSize);

            var result = await _repo.GetDevicesAsync(filter, pagination);

            activity?.SetTag("result.count", result.Items.Count);

            var dtoItems = result.Items
                .Select(d => d.ToDto())
                .ToList();

            return new PaginatedResult<DeviceDto>(
                dtoItems,
                result.TotalCount,
                result.PageNumber,
                result.PageSize
            );
        }
    }
}

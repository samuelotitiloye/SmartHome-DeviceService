using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Mappings;
using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Queries;   
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

        public async Task<IEnumerable<DeviceDto>> GetAllAsync(CancellationToken ct)
        {
            var devices = await _repo.GetAllAsync();
            return devices.Select(d => d.ToDto());
        }

        public async Task<PagedResult<DeviceDto>> GetPagedDtoAsync(
            int page,
            int pageSize,
            string? type,
            string? location,
            bool? isOnline,
            CancellationToken ct)
        {
            var paged = await _repo.GetPagedAsync(page, pageSize, type, location, isOnline);

            return new PagedResult<DeviceDto>
            {
                Items = paged.Items.Select(d => d.ToDto()),
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalItems = paged.TotalItems
            };
        }
    }
}

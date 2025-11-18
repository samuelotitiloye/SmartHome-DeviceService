using DeviceService.Application.Dto;
using DeviceService.Application.Mappings;
using DeviceService.Application.Interfaces;

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
    }
}

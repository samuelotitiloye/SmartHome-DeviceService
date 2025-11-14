using DeviceService.Application.Dto;
using DeviceService.Application.Interfaces;
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

        public async Task<Device> RegisterDeviceAsync(RegisterDeviceDto dto, CancellationToken ct = default)
        {
            var device = new Device(
                dto.DeviceName,
                dto.Type,
                dto.Location,
                dto.ThresholdWatts
            );

            await _repo.AddAsync(device, ct);
            return device;
        }
    }
    
}
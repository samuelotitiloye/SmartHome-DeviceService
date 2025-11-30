using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Dto;
using DeviceService.Domain.Entities;
using DeviceService.Infrastructure.Cache;
using MediatR;
using Serilog;

namespace DeviceService.Application.Devices.Commands.RegisterDevice
{
    public class RegisterDeviceCommandHandler 
        : IRequestHandler<RegisterDeviceCommand, DeviceDto>
    {
        private readonly IDeviceRepository _repo;
        private readonly RedisCacheService _cache;

        public RegisterDeviceCommandHandler(IDeviceRepository repo, RedisCacheService cache)
        {
            _repo = repo;
            _cache = cache;
        }

        public async Task<DeviceDto> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
        {
            Log.Information("Registering device {@Request}", request);

            var device = new Device
            {
                Name = request.Name,
                Type = request.Type,
                Location = request.Location,
                ThresholdWatts = request.ThresholdWatts,
                SerialNumber = request.SerialNumber,
                IsOnline = false,
                RegisteredAt = DateTime.UtcNow
            };

            await _repo.AddAsync(device);

            Log.Information("Device registered successfully {@Device}", device);

            // ===============================================
            // REDIS CACHE INVALIDATION
            // ===============================================

            // Invalidate single device
            await _cache.RemoveAsync($"device:{device.Id}");

            //invalidate paginated device list
            await InvalidateDeviceListCache();

            return new DeviceDto(
                device.Id,
                device.Name,
                device.Type,
                device.Location,
                device.IsOnline,
                device.ThresholdWatts,
                device.SerialNumber,
                device.RegisteredAt
            );
        }

        private async Task InvalidateDeviceListCache()
        {
            for (int page = 1; page <= 5; page++)
            {
                var keyPrefix = $"devices: {page}:";
                await _cache.RemoveAsync(keyPrefix);
            }
        }
    }
}

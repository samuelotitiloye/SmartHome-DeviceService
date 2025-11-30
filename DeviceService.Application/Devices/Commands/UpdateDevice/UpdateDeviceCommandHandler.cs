using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Dto;
using DeviceService.Domain.Entities;
using DeviceService.Infrastructure.Cache;
using MediatR;
using Serilog;

namespace DeviceService.Application.Devices.Commands.UpdateDevice
{
    public class UpdateDeviceCommandHandler 
        : IRequestHandler<UpdateDeviceCommand, DeviceDto?>
    {
        private readonly IDeviceRepository _repo;
        private readonly RedisCacheService _cache;

        public UpdateDeviceCommandHandler(IDeviceRepository repo, RedisCacheService cache)
        {
            _repo = repo;
            _cache = cache;
        }

        public async Task<DeviceDto?> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
        {
            // Fetch device device
            var device = await _repo.GetByIdAsync(request.Id);
            if (device is null)
            {
                Log.Warning("Device {DeviceId} not found for update", request.Id);
                return null;
            }

            // Apply updates
            device.Name = request.Name;
            device.Type = request.Type;
            device.Location = request.Location;
            device.IsOnline = request.IsOnline;
            device.ThresholdWatts = request.ThresholdWatts;
            device.SerialNumber = request.SerialNumber;

            await _repo.UpdateAsync(device);

            Log.Information("Device updated successfully {@Device}", device);

            // if (request.ThresholdWatts.HasValue)
            // {
            //     device.ThresholdWatts = request.ThresholdWatts.Value;
            // }
            // device.SerialNumber = request.SerialNumber;

            // ===============================================
            // REDIS CACHE INVALIDATION
            // ===============================================

            // clear single device cache
            await _cache.RemoveAsync($"device:{device.Id}");
            
            // Clear list caches
            await InvalidateDeviceListCache();

            // Return DTO
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

        private async Task ?InvalidateDeviceListCache()
        {
            // Simple predictable invalidation strategy (pages 1–5). SCAN/DEL to come
            for (int page = 1; page <= 5; page++)
            {
                var keyPrefix = $"devices:{page}:";
                await _cache.RemoveAsync(keyPrefix);
            }
        }
    }
}
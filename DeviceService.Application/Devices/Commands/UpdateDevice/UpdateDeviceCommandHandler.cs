using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Dto;
using MediatR;
using Serilog;

namespace DeviceService.Application.Devices.Commands.UpdateDevice
{
    public class UpdateDeviceCommandHandler 
        : IRequestHandler<UpdateDeviceCommand, DeviceDto?>
    {
        private readonly IDeviceRepository _repo;
        private readonly ICacheService _cache;

        public UpdateDeviceCommandHandler(IDeviceRepository repo, ICacheService cache)
        {
            _repo = repo;
            _cache = cache;
        }

        public async Task<DeviceDto?> Handle(UpdateDeviceCommand request, CancellationToken ct)
        {
            Log.Information("Updating device {@Request}", request);
            // Fetch device device
            var device = await _repo.GetByIdAsync(request.Id);
            if (device == null)
            {
                Log.Warning("Device {DeviceId} not found for update", request.Id);
                return null;
            }

            // Apply updates
            device.Name = request.Name;
            device.Type = request.Type;
            device.Location = request.Location;
            device.IsOnline = request.IsOnline;
            device.ThresholdWatts = request.ThresholdWatts ?? device.ThresholdWatts;
            device.SerialNumber = request.SerialNumber;

            await _repo.UpdateAsync(device);

            Log.Information("Device updated successfully {@Device}", device);

            // ===============================================
            // REDIS CACHE INVALIDATION
            // ===============================================

            // clear single device cache
            await _cache.RemoveAsync($"device:{device.Id}");
            
            // Clear list caches
            await _cache.RemoveByPatternAsync("devices:*");

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
    }
}
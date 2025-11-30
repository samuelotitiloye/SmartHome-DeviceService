using DeviceService.Application.Interfaces;
using DeviceService.Application.Cache;
using MediatR;
using Serilog;

namespace DeviceService.Application.Devices.Commands.DeleteDevice
{
    public class DeleteDeviceCommandHandler : IRequestHandler<DeleteDeviceCommand, bool>
    {
        private readonly IDeviceRepository _repo;
        private readonly RedisCacheService _cache;

        public DeleteDeviceCommandHandler(IDeviceRepository repo, RedisCacheService cache)
        {
            _repo = repo;
            _cache = cache;
        }
        public async Task<bool> Handle(DeleteDeviceCommand request, CancellationToken ct)
        {
            Log.Information("Deleting device with ID {DeviceId}", request.Id);

            var device = await _repo.GetByIdAsync(request.Id);
            if (device == null)
            {
                Log.Warning("Device {DeviceId} not found", request.Id);
                return false;
            }

            // Perform deletion (throws if it fails)
            await _repo.DeleteAsync(request.Id);

            Log.Information("Device deleted successfully {@Device}", device);

            // ===============================
            // Redis cache invalidation
            // ===============================

            // delete single device cache key
            await _cache.RemoveAsync($"device:{device.Id}");

            // delete device list cache prefixes
            await InvalidateDeviceListCache();

            return true;
        }

        private async Task InvalidateDeviceListCache()
        {
            for (int page = 1; page <= 5; page++)
            {
                await _cache.RemoveAsync($"devices:{page}:");
            }
        }
    }
}

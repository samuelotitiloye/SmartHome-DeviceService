using DeviceService.Application.Interfaces;
using DeviceService.Infrastructure.Cache;
using MediatR;
using Serilog;

namespace DeviceService.Application.Devices.Commands.DeleteDevice
{
    public class DeleteDeviceCommandHandler : IRequestHandler<DeleteDeviceCommand, bool>
    {
        private readonly IDeviceRepository _repo;
        private readonly RedisCacheService cache;

        public DeleteDeviceCommandHandler(IDeviceRepository repo)
        {
            _repo = repo;
            _cache = cache;
        }

        public async Task<bool> Handle(DeleteDeviceCommand request, CancellationToken ct)
        {
            Log.Information("Deleting device with ID {Device.Id}", request.request.Id);

            var device = await _repo.GetByIdAsync(request.Id);
            if (device == null)
            {
                Log.Warning("Device {DeviceId} not found for deletion", request.Id);
                return false;
            }

            var deleted = await _repo.DeleteAsync(request.Id);
            if (!deleted)
            {
                Log.Warning("Failed to delete device {DeviceId}", request.Id);
                return false;
            }
            Log.Information("Device deleted successfully {@Device}", device);
                

            // ===============================================
            // REDIS CACHE INVALIDATION
            // ===============================================
            // single
            await _cache.RemoveAsync($"device:{device.Id}");

            // invalidate paged device list
            await InvalidateDeviceListCache();

            return true;
        }

        private async Task InvalidateDeviceListCache()
        {
            for (int page = 1; page <= 5; page++)
            {
                var keyPrefix = $"device:{page}:";
                await _cache.RemoveAsync(keyPrefix);
            }
        }
    }
}

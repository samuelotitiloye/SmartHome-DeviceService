using MediatR;
using DeviceService.Application.Devices.Caching;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Devices.Commands.UpdateDevice;

namespace DeviceService.Application.Devices.Commands.UpdateDevice
{
    /// <summary>
    /// Decorator that invalidates device-related cache entries after a successful update.
    /// </summary>
    public class UpdateDeviceCacheInvalidationHandler
        : IRequestHandler<UpdateDeviceCommand, DeviceDto?>
    {
        private readonly IRequestHandler<UpdateDeviceCommand, DeviceDto?> _inner;
        private readonly DeviceCacheInvalidator _invalidator;

        public UpdateDeviceCacheInvalidationHandler(
            IRequestHandler<UpdateDeviceCommand, DeviceDto?> inner,
            DeviceCacheInvalidator invalidator)
        {
            _inner = inner;
            _invalidator = invalidator;
        }

        public async Task<DeviceDto?> Handle(UpdateDeviceCommand request, CancellationToken ct)
        {
            var result = await _inner.Handle(request, ct);

            if (result is not null)
            {
                await _invalidator.RemoveForDevice(request.Id);
                await _invalidator.RemoveList();
            }

            return result;
        }
    }
}

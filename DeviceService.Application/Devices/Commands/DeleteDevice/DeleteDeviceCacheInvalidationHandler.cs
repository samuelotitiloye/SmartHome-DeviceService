using MediatR;
using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Caching;
using DeviceService.Application.Devices.Commands.DeleteDevice;


public class DeleteDeviceCacheInvalidationHandler : IRequestHandler<DeleteDeviceCommand, bool>
{
    private readonly IRequestHandler<DeleteDeviceCommand, bool> _inner;
    private readonly DeviceCacheInvalidator _invalidator;
    
    public DeleteDeviceCacheInvalidationHandler(IRequestHandler<DeleteDeviceCommand, bool> inner, DeviceCacheInvalidator invalidator)
    {
        _inner = inner;
        _invalidator = invalidator;
    }

    public async Task<bool> Handle(DeleteDeviceCommand request, CancellationToken ct)
    {
        var result = await _inner.Handle(request, ct);

        if (result)
        {
            await _invalidator.RemoveForDevice(request.Id);
            await _invalidator.RemoveList();
        }

        return result;
    }
}
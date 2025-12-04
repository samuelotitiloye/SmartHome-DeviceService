using MediatR;
using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Caching;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Devices.Commands.RegisterDevice;


public class RegisterDeviceCacheInvalidationHandler :IRequestHandler<RegisterDeviceCommand, DeviceDto>
{
    private readonly IRequestHandler<RegisterDeviceCommand, DeviceDto> _inner;
    private readonly DeviceCacheInvalidator _invalidator;

    public RegisterDeviceCacheInvalidationHandler(IRequestHandler<RegisterDeviceCommand, DeviceDto> inner, DeviceCacheInvalidator invalidator)
    {
        _inner = inner;
        _invalidator = invalidator;
    }

    public async Task<DeviceDto> Handle(RegisterDeviceCommand request, CancellationToken ct)
    {
        var result = await _inner.Handle(request, ct);
        
        await _invalidator.RemoveList();

        return result;
    }
}
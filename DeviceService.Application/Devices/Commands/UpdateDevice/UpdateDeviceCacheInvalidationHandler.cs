using MediatR;
using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Caching;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Devices.Commands.UpdateDevice;


public class UpdateDeviceCacheInvalidationHandler : IRequestHandler<UpdateDeviceCommand, DeviceDto>
{
    private readonly IRequestHandler<UpdateDeviceCommand, DeviceDto> _inner;
    private readonly DeviceCacheInvalidator _invalidator;

    public UpdateDeviceCacheInvalidationHandler(IRequestHandler<UpdateDeviceCommand, DeviceDto> inner, DeviceCacheInvalidator invalidator)
    {
        _inner = inner;
        _invalidator = invalidator;
    }

    public async Task<DeviceDto> Handle(UpdateDeviceCommand request, CancellationToken ct)
    {
        var result = await _inner.Handle(request, ct);

        await _invalidator.RemoveForDevice(request.Id);
        await _invalidator.RemoveList();

        return result;
    }
}
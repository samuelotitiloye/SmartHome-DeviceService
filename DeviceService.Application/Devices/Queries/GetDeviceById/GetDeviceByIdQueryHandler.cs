using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Interfaces;
using MediatR;

namespace DeviceService.Application.Devices.Queries.GetDeviceById
{
    public class GetDeviceByIdQueryHandler 
        : IRequestHandler<GetDeviceByIdQuery, DeviceDto?>
    {
        private readonly IDevicesService _service;

        public GetDeviceByIdQueryHandler(IDevicesService service)
        {
            _service = service;
        }

        public async Task<DeviceDto?> Handle(GetDeviceByIdQuery request, CancellationToken ct)
        {
            return await _service.GetByIdAsync(request.Id, ct);
        }
    }
}

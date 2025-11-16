using MediatR;
using DeviceService.Domain.Repositories;
using DeviceService.Application.Dto;
using DeviceService.Application.Mappings;

namespace DeviceService.Application.Devices.Commands.UpdateDevice
{
    public class UpdateDeviceCommandHandler 
        : IRequestHandler<UpdateDeviceCommand, DeviceDto?>
    {
        private readonly IDeviceRepository _repo;

        public UpdateDeviceCommandHandler(IDeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task<DeviceDto?> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
        {
            var existing = await _repo.GetByIdAsync(request.Id);
            if (existing is null)
                return null;

            existing.ApplyUpdate(request);

            var updated = await _repo.UpdateAsync(existing);

            return updated?.ToDto();
        }
    }
}

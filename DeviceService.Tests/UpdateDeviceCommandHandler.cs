using MediatR;
using DeviceService.Domain.Repositories;
using DeviceService.Application.Dto;
using DeviceService.Application.Mappings;


namespace DeviceService.Application.Devices.Commands.UpdateDevice
{
    public class UpdateDeviceCommandHandler : IRequestHandler<UpdateDeviceCommand, DeviceDto?>
    {
        private readonly IDeviceRepository _repository;

        public UpdateDeviceCommandHandler(IDeviceRepository repository)
        {
            _repository = repository;
        }

        public async Task<DeviceDto?> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
        {
            var device = await _repository.GetByIdAsync(request.Id);

            if (device is null)
                return null;

            //FORCE compiler to see the correct overload
            device.ApplyUpdate(request);

            await _repository.UpdateAsync(device);

            return device.ToDto();
        }
    }
}

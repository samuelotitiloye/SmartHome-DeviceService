using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Interfaces;
using DeviceService.Domain.Entities;
using MediatR;

namespace DeviceService.Application.Devices.Commands.UpdateDevice
{
    public class UpdateDeviceCommandHandler 
        : IRequestHandler<UpdateDeviceCommand, DeviceDto?>
    {
        private readonly IDeviceRepository _repository;

        public UpdateDeviceCommandHandler(IDeviceRepository repository)
        {
            _repository = repository;
        }

        public async Task<DeviceDto?> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
        {
            // Fetch existing device
            var existing = await _repository.GetByIdAsync(request.Id);
            if (existing == null)
                return null;

            // Apply updates
            existing.Name = request.Name;
            existing.Type = request.Type;
            existing.Location = request.Location;
            existing.IsOnline = request.IsOnline;
            existing.ThresholdWatts = request.ThresholdWatts;
            existing.SerialNumber = request.SerialNumber;

            // Save changes
            await _repository.UpdateAsync(existing);

            // Return DTO
            return new DeviceDto(
                existing.Id,
                existing.Name,
                existing.Type,
                existing.Location,
                existing.IsOnline,
                existing.ThresholdWatts,
                existing.SerialNumber,
                existing.RegisteredAt
            );
        }
    }
}

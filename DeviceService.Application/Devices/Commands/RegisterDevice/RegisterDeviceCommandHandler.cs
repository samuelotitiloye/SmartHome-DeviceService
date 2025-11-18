using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Dto;
using DeviceService.Domain.Entities;
using MediatR;
using Serilog;

namespace DeviceService.Application.Devices.Commands.RegisterDevice
{
    public class RegisterDeviceCommandHandler 
        : IRequestHandler<RegisterDeviceCommand, DeviceDto>
    {
        private readonly IDeviceRepository _repo;

        public RegisterDeviceCommandHandler(IDeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task<DeviceDto> Handle(
            RegisterDeviceCommand request, 
            CancellationToken cancellationToken)
        {
            Log.Information("Registering device {@Request}", request);

            var device = new Device
            {
                Name = request.Name,
                Type = request.Type,
                Location = request.Location,
                ThresholdWatts = request.ThresholdWatts,
                SerialNumber = request.SerialNumber,
                IsOnline = false,
                RegisteredAt = DateTime.UtcNow
            };

            await _repo.AddAsync(device);

            Log.Information("Device registered successfully {@Device}", device);

            return new DeviceDto(
                device.Id,
                device.Name,
                device.Type,
                device.Location,
                device.IsOnline,
                device.ThresholdWatts,
                device.SerialNumber,
                device.RegisteredAt
            );
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using DeviceService.Application.Interfaces;
using DeviceService.Application.Devices.Dto;
using DeviceService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DeviceService.Application.Devices.Commands.RegisterDevice
{
    public class RegisterDeviceCommandHandler 
        : IRequestHandler<RegisterDeviceCommand, DeviceDto>
    {
        private readonly IDeviceRepository _repo;
        private readonly ICacheService _cache;
        private readonly ILogger<RegisterDeviceCommandHandler> _logger;

        public RegisterDeviceCommandHandler(
            IDeviceRepository repo,
            ICacheService cache,
            ILogger<RegisterDeviceCommandHandler> logger)
        {
            _repo = repo;
            _cache = cache;
            _logger = logger;
        }

        public async Task<DeviceDto> Handle(
            RegisterDeviceCommand request, 
            CancellationToken ct)
        {
            _logger.LogInformation("Registering device {@Request}", request);

            var device = new Device
            {
                Name = request.Name,
                Type = request.Type,
                Location = request.Location,
                IsOnline = request.IsOnline,
                ThresholdWatts = request.ThresholdWatts,
                SerialNumber = request.SerialNumber,
                RegisteredAt = DateTime.UtcNow
            };

            await _repo.AddAsync(device);

            // Cache invalidation
            await _cache.RemoveByPatternAsync("devices:*");
            await _cache.RemoveAsync($"device:{device.Id}");

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

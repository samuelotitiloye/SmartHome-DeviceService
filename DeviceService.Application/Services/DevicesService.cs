using DeviceService.Application.Dto;
using DeviceService.Domain.Entities;
using DeviceService.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace DeviceService.Application.Services
{
    public class DevicesService
    {
        private readonly IDeviceRepository _repo;
        private readonly ILogger<DevicesService> _logger;

        public DevicesService(IDeviceRepository repo, ILogger<DevicesService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        private static DeviceResponseDto MapToDto(Device device)
        {
            return new DeviceResponseDto
            {
                Id = device.Id,
                DeviceName = device.DeviceName,
                Type = device.Type,
                Location = device.Location,
                ThresholdWatts = device.ThresholdWatts,
                SerialNumber = device.SerialNumber,
                RegisteredAt = device.RegisteredAt,
            };
        }

        public async Task<DeviceResponseDto> RegisterDeviceAsync(RegisterDeviceDto dto, CancellationToken ct = default)
        {
            ValidationRegisterDeviceDto(dto);

            _logger.LogDebug("Registering device {DeviceName}", dto.DeviceName);

            var device = new Device(
                dto.DeviceName,
                dto.Type,
                dto.Location,
                dto.ThresholdWatts,
                dto.SerialNumber
            );

            await _repo.AddAsync(device);

            _logger.LogDebug("Device registered with Id={Id}", device.Id);

            return MapToDto(device);
        }

        public async Task<DeviceResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            _logger.LogDebug("Fetching device by Id={Id}", id);

            var device = await _repo.GetByIdAsync(id);

            return device is null ? null : MapToDto(device);
        }

        public async Task<IEnumerable<DeviceResponseDto>> GetAllAsync(CancellationToken ct = default)
        {
            _logger.LogDebug("Fetching all devices");

            var devices = await _repo.GetAllAsync();

            return devices.Select(MapToDto);
        }

        private static void ValidationRegisterDeviceDto(RegisterDeviceDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.DeviceName))
                throw new ArgumentException("DeviceName is required.");

            if (string.IsNullOrWhiteSpace(dto.Type))
                throw new ArgumentException("Type is required.");

            if (string.IsNullOrWhiteSpace(dto.SerialNumber))
                throw new ArgumentException("SerialNumber is required.");

            if (dto.ThresholdWatts <= 0)
                throw new ArgumentException("ThresholdWatts must be greater than zero.");
        }
    }
}

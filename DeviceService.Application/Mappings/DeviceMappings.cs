using DeviceService.Application.Devices.Dto;
using DeviceService.Domain.Entities;

namespace DeviceService.Application.Mappings
{
    /// <summary>
    /// Provides mapping extensions for converting between domain entities and DTOs.
    /// </summary>
    public static class DeviceMappings
    {
        /// <summary>
        /// Converts a <see cref="Device"/> domain entity to a <see cref="DeviceDto"/>.
        /// </summary>
        public static DeviceDto ToDto(this Device device)
        {
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

        /// <summary>
        /// Converts a <see cref="DeviceDto"/> to a <see cref="Device"/> domain entity.
        /// Useful for update operations via DTO.
        /// </summary>
        public static Device ToEntity(this DeviceDto dto)
        {
            return new Device
            {
                Id = dto.Id,
                Name = dto.Name,
                Type = dto.Type,
                Location = dto.Location,
                IsOnline = dto.IsOnline,
                ThresholdWatts = dto.ThresholdWatts ?? 0,
                SerialNumber = dto.SerialNumber ?? string.Empty,
                RegisteredAt = dto.RegisteredAt
            };
        }

        /// <summary>
        /// Converts a <see cref="RegisterDeviceDto"/> to a new <see cref="Device"/> entity.
        /// Used during device registration.
        /// </summary>
        public static Device ToEntity(this RegisterDeviceDto dto)
        {
            return new Device
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Type = dto.Type,
                Location = dto.Location,
                IsOnline = dto.IsOnline,
                ThresholdWatts = dto.ThresholdWatts ?? 0,
                SerialNumber = dto.SerialNumber ?? string.Empty,
                RegisteredAt = DateTime.UtcNow
            };
        }
    }
}

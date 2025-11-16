using DeviceService.Domain.Entities;
using DeviceService.Application.Dto;
using DeviceService.Application.Devices.Commands.UpdateDevice;

namespace DeviceService.Application.Mappings
{
    public static class DeviceMappings
    {
        public static DeviceDto ToDto(this Device d)
        {
            return new DeviceDto
            {
                Id = d.Id,
                Name = d.Name,
                Type = d.Type,
                Location = d.Location,
                IsOnline = d.IsOnline,
                ThresholdWatts = d.ThresholdWatts,
                SerialNumber = d.SerialNumber,
                RegisteredAt = d.RegisteredAt
            };
        }

        public static Device ToEntity(this RegisterDeviceDto dto)
        {
            return new Device
            {
                Id = Guid.NewGuid(),
                Name = dto.DeviceName,
                Type = dto.Type,
                Location = dto.Location,
                IsOnline = true,
                ThresholdWatts = dto.ThresholdWatts,
                SerialNumber = dto.SerialNumber,
                RegisteredAt = DateTime.UtcNow
            };
        }

        public static void ApplyUpdate(this Device d, UpdateDeviceCommand cmd)
        {
            d.Name = cmd.Name;
            d.Type = cmd.Type;
            d.Location = cmd.Location;
            d.IsOnline = cmd.IsOnline;
            d.ThresholdWatts = cmd.ThresholdWatts;
            d.SerialNumber = cmd.SerialNumber;
        }
    }
}

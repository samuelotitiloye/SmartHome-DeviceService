using DeviceService.Application.Devices.Dto;
using DeviceService.Domain.Entities;

namespace DeviceService.Application.Mappings
{
    public static class DeviceMappings
    {
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

        public static Device ToEntity(this DeviceDto dto)
        {
            return new Device
            {
                Id = dto.Id,
                Name = dto.Name,
                Type = dto.Type,
                Location = dto.Location,
                IsOnline = dto.IsOnline,
                ThresholdWatts = dto.ThresholdWatts,
                SerialNumber = dto.SerialNumber
            };
        }

        // RegisterDeviceDto → Device
        public static Device ToEntity(this RegisterDeviceDto dto)
        {
            return new Device
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Type = dto.Type,
                Location = dto.Location,
                IsOnline = dto.IsOnline,
                ThresholdWatts = dto.ThresholdWatts,
                SerialNumber = dto.SerialNumber
            };
        }
    }
}

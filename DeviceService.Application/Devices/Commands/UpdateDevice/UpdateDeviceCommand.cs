using MediatR;
using DeviceService.Application.Dto;

namespace DeviceService.Application.Devices.Commands.UpdateDevice
{
    public class UpdateDeviceCommand : IRequest<DeviceDto?>
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public bool IsOnline { get; init; }
        public int ThresholdWatts { get; init; }
        public string SerialNumber { get; init; } = string.Empty;

        // PRIMARY CONSTRUCTOR ALTERNATIVE
        public UpdateDeviceCommand(
            Guid id,
            string name,
            string type,
            string location,
            bool isOnline,
            int thresholdWatts,
            string serialNumber)
        {
            Id = id;
            Name = name;
            Type = type;
            Location = location;
            IsOnline = isOnline;
            ThresholdWatts = thresholdWatts;
            SerialNumber = serialNumber;
        }
    }
}

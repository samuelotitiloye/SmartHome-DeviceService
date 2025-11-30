using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Devices.Models;
using DeviceService.Application.Common.Models;

namespace DeviceService.Application.Interfaces
{
    public interface IDevicesService
    {
        Task<DeviceDto?> GetByIdAsync(Guid id, CancellationToken ct);

        Task<PaginatedResult<DeviceDto>> GetDevicesAsync(
            DeviceFilter filter,
            PaginationParameters pagination,
            CancellationToken ct = default
        );

        Task<DeviceDto> RegisterDeviceAsync(RegisterDeviceDto dto, CancellationToken ct);
    }
}

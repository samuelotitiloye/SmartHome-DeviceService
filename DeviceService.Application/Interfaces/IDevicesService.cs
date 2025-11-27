using DeviceService.Application.Common.Models;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Devices.Models;
using DeviceService.Domain.Entities;

namespace DeviceService.Application.Interfaces
{
    public interface IDevicesService
    {
        Task<DeviceDto> RegisterDeviceAsync(RegisterDeviceDto dto, CancellationToken ct);

        Task<DeviceDto?> GetByIdAsync(Guid id, CancellationToken ct);

        Task<PaginatedResult<Device>> GetPagedAsync(
            int page,
            int pageSize,
            string? type,
            string? location,
            bool? isOnline,
            CancellationToken cancellationToken = default
        );

        Task<PaginatedResult<DeviceDto>> GetDevicesAsync(
            DeviceFilter filter,
            PaginationParameters pagination,
            CancellationToken cancellationToken = default
        );
    }
}

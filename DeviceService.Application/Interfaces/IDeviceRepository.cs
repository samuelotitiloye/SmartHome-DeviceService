using DeviceService.Application.Common.Models;
using DeviceService.Application.Devices.Models;
using DeviceService.Domain.Entities;

namespace DeviceService.Application.Interfaces
{
    public interface IDeviceRepository
    {
        // New pagination methods
        Task<PaginatedResult<Device>> GetDevicesAsync(
            DeviceFilter filter,
            PaginationParameters pagination,
            CancellationToken cancellationToken = default);

        Task<int> GetDevicesCountAsync(
            DeviceFilter filter,
            CancellationToken cancellationToken = default);

        // ------------------------------
        // CRUD operations
        // ------------------------------
        Task<Device?> GetByIdAsync(Guid id);
        Task AddAsync(Device device);
        Task UpdateAsync(Device device);
        Task DeleteAsync(Guid id);
    }
}

using DeviceService.Domain.Entities;
using DeviceService.Application.Devices.Queries;

namespace DeviceService.Application.Interfaces
{
    public interface IDeviceRepository
    {
        Task<Device?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<Device>> GetAllAsync();
        Task AddAsync(Device device);
        Task UpdateAsync(Device device);
        Task DeleteAsync(Guid id);
        Task <PagedResult<Device>> GetPagedAsync(
            int page,
            int pageSize,
            string? type,
            string? location,
            bool? isOnline);
    }
}

using DeviceService.Domain.Entities;

namespace DeviceService.Application.Interfaces
{
    public interface IDeviceRepository
    {
        Task<Device?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<Device>> GetAllAsync();
        Task AddAsync(Device device);
        Task UpdateAsync(Device device);
        Task DeleteAsync(Guid id);
    }
}

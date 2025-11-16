using DeviceService.Domain.Entities;

namespace DeviceService.Domain.Repositories
{
    public interface IDeviceRepository
    {
        Task<Device> AddAsync(Device device);
        Task<Device?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<Device>> GetAllAsync();
        Task<Device?> UpdateAsync(Device device);
        Task<bool> DeleteAsync(Guid id);
    }
}

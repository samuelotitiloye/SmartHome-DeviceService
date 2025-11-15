using DeviceService.Domain.Entities;

namespace DeviceService.Domain.Repositories
{
    public interface IDeviceRepository
    {
        Task AddAsync(Device device);
        Task<Device?> GetByIdAsync(Guid id);
        Task<IEnumerable<Device>> GetAllAsync();
    }
}

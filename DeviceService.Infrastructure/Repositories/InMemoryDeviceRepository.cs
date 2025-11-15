using DeviceService.Domain.Entities;
using DeviceService.Domain.Repositories;

namespace DeviceService.Infrastructure.Repositories
{
    public class InMemoryDeviceRepository : IDeviceRepository
    {
        private readonly Dictionary<Guid, Device> _store = new();

        public Task AddAsync(Device device)
        {
            _store[device.Id] = device;
            return Task.CompletedTask;
        }

        public Task<Device?> GetByIdAsync(Guid id)
        {
            _store.TryGetValue(id, out var device);
            return Task.FromResult(device);
        }

        public Task<IEnumerable<Device>> GetAllAsync()
        {
            return Task.FromResult(_store.Values.AsEnumerable());
        }
    }
}

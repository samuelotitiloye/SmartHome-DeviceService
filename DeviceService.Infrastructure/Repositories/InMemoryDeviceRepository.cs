using DeviceService.Application.Interfaces;
using DeviceService.Domain.Entities;

namespace DeviceService.Infrastructure.Repositories
{
    public class InMemoryDeviceRepository : IDeviceRepository
    {
        private readonly List<Device> _devices = [];

        public Task<Device?> GetByIdAsync(Guid id)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            return Task.FromResult(device);
        }

        public Task<IReadOnlyList<Device>> GetAllAsync()
        {
            IReadOnlyList<Device> devices = _devices.ToList();
            return Task.FromResult(devices);
        }

        public Task AddAsync(Device device)
        {
            _devices.Add(device);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Device device)
        {
            var existing = _devices.FirstOrDefault(d => d.Id == device.Id);
            if (existing != null)
            {
                _devices.Remove(existing);
                _devices.Add(device);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            var existing = _devices.FirstOrDefault(d => d.Id == id);
            if (existing != null)
            {
                _devices.Remove(existing);
            }

            return Task.CompletedTask;
        }
    }
}

using DeviceService.Domain.Entities;
using DeviceService.Domain.Repositories;

namespace DeviceService.Infrastructure.Repositories
{
    public class InMemoryDeviceRepository : IDeviceRepository
    {
        private readonly List<Device> _devices = new();

        public Task<Device> AddAsync(Device device)
        {
            if (device.Id == Guid.Empty)
                device.Id = Guid.NewGuid();

            if (device.RegisteredAt == default)
                device.RegisteredAt = DateTime.UtcNow;

            _devices.Add(device);
            return Task.FromResult(device);
        }

        public Task<Device?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_devices.FirstOrDefault(d => d.Id == id));
        }

        public Task<IReadOnlyList<Device>> GetAllAsync()
        {
            return Task.FromResult((IReadOnlyList<Device>)_devices.ToList());
        }

        public Task<Device?> UpdateAsync(Device device)
        {
            var index = _devices.FindIndex(x => x.Id == device.Id);
            if (index < 0)
                return Task.FromResult<Device?>(null);

            _devices[index] = device;
            return Task.FromResult<Device?>(device);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            var existing = _devices.FirstOrDefault(x => x.Id == id);
            if (existing is null)
                return Task.FromResult(false);

            _devices.Remove(existing);
            return Task.FromResult(true);
        }
    }
}

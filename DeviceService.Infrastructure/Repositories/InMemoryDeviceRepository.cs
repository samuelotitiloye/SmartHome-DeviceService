using System.Collections.Concurrent;
using DeviceService.Domain.Entities;
using DeviceService.Application.Interfaces;

namespace DeviceService.Infrastructure.Repositories
{
    public class InMemoryDeviceRepository : IDeviceRepository
    {
        private readonly ConcurrentDictionary<Guid, Device> _store = new();

        public Task AddAsync(Device device, CancellationToken ct = default)
        {
            _store[device.Id] = device;
            return Task.CompletedTask;
        }

        public Task<Device?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            _store.TryGetValue(id, out var device);
            return Task.FromResult(device);
        }
    }
}
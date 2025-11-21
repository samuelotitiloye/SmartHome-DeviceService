using DeviceService.Application.Interfaces;
using DeviceService.Domain.Entities;
using DeviceService.Application.Devices.Queries;   
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

        public Task<PagedResult<Device>> GetPagedAsync(
            int page,
            int pageSize,
            string? type,
            string? location,
            bool? isOnline)
        {
            IEnumerable<Device> query = _devices;

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(d => d.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(d => d.Location.Equals(location, StringComparison.OrdinalIgnoreCase));

            if (isOnline.HasValue)
                query = query.Where(d => d.IsOnline == isOnline.Value);

            var totalItems = query.Count();

            var items = query
                .OrderBy(d => d.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult(new PagedResult<Device>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            });
        }

    }
}

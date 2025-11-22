using DeviceService.Application.Interfaces;
using DeviceService.Application.Common.Models;
using DeviceService.Application.Devices.Models;
using DeviceService.Domain.Entities;

namespace DeviceService.Infrastructure.Repositories
{
    public class InMemoryDeviceRepository : IDeviceRepository
    {
        private readonly List<Device> _devices = new();

        public Task AddAsync(Device device)
        {
            _devices.Add(device);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            var device = _devices.FirstOrDefault(x => x.Id == id);
            if (device != null)
                _devices.Remove(device);

            return Task.CompletedTask;
        }

        public Task<Device?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_devices.FirstOrDefault(d => d.Id == id));
        }

        public Task UpdateAsync(Device device)
        {
            var index = _devices.FindIndex(d => d.Id == device.Id);
            if (index >= 0)
                _devices[index] = device;

            return Task.CompletedTask;
        }

        public Task<PaginatedResult<Device>> GetDevicesAsync(
            DeviceFilter filter,
            PaginationParameters pagination,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Device> query = _devices;

            // Filters
            if (!string.IsNullOrWhiteSpace(filter.NameContains))
                query = query.Where(d => d.Name.Contains(filter.NameContains));

            if (!string.IsNullOrWhiteSpace(filter.Location))
                query = query.Where(d => d.Location == filter.Location);

            if (!string.IsNullOrWhiteSpace(filter.Type))
                query = query.Where(d => d.Type == filter.Type);

            if (filter.IsOnline.HasValue)
                query = query.Where(d => d.IsOnline == filter.IsOnline.Value);

            if (filter.MinThresholdWatts.HasValue)
                query = query.Where(d => d.ThresholdWatts >= filter.MinThresholdWatts.Value);

            var filtered = query.ToList();
            var total = filtered.Count;

            if (total == 0)
                return Task.FromResult(PaginatedResult<Device>.Empty(pagination));

            // Pagination
            var skip = (pagination.PageNumber - 1) * pagination.PageSize;
            var items = filtered
                .Skip(skip)
                .Take(pagination.PageSize)
                .ToList();

            return Task.FromResult(new PaginatedResult<Device>(
                items,
                pagination.PageNumber,
                pagination.PageSize,
                total));
        }

        public Task<int> GetDevicesCountAsync(
            DeviceFilter filter,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Device> query = _devices;

            if (!string.IsNullOrWhiteSpace(filter.NameContains))
                query = query.Where(d => d.Name.Contains(filter.NameContains));

            if (!string.IsNullOrWhiteSpace(filter.Location))
                query = query.Where(d => d.Location == filter.Location);

            if (!string.IsNullOrWhiteSpace(filter.Type))
                query = query.Where(d => d.Type == filter.Type);

            if (filter.IsOnline.HasValue)
                query = query.Where(d => d.IsOnline == filter.IsOnline.Value);

            if (filter.MinThresholdWatts.HasValue)
                query = query.Where(d => d.ThresholdWatts >= filter.MinThresholdWatts.Value);

            return Task.FromResult(query.Count());
        }
    }
}

using DeviceService.Application.Interfaces;
using DeviceService.Application.Common.Models;
using DeviceService.Application.Devices.Models;
using DeviceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DeviceService.Domain.Entities;

namespace DeviceService.Infrastructure.Repositories
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly DeviceDbContext _context;

        public DeviceRepository(DeviceDbContext context)
        {
            _context = context;
        }

        // ============================================================
        //   MAIN PAGINATED QUERY 
        // ============================================================
        public async Task<PaginatedResult<Device>> GetDevicesAsync(
            DeviceFilter filter,
            PaginationParameters pagination,
            CancellationToken cancellationToken = default)
        {
            var query = BuildFilteredQuery(filter);

            var totalCount = await query.CountAsync(cancellationToken);

            if (totalCount == 0)
                return PaginatedResult<Device>.Empty(pagination);

            // Normalize page number
            var maxPage = (int)Math.Ceiling(totalCount / (double)pagination.PageSize);
            var currentPage = Math.Clamp(pagination.PageNumber, 1, maxPage);

            query = ApplySorting(query, filter);

            var skip = (currentPage - 1) * pagination.PageSize;

            var items = await query
                .Skip(skip)
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResult<Device>(
                items,
                currentPage,
                pagination.PageSize,
                totalCount);
        }

        // ============================================================
        //   Count Endpoint Support
        // ============================================================
        public Task<int> GetDevicesCountAsync(
            DeviceFilter filter,
            CancellationToken cancellationToken = default)
        {
            return BuildFilteredQuery(filter).CountAsync(cancellationToken);
        }

        // ============================================================
        //   QUERY BUILDING HELPERS
        // ============================================================
        private IQueryable<Device> BuildFilteredQuery(DeviceFilter filter)
        {
            IQueryable<Device> query = _context.Devices.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.NameContains))
            {
                var term = filter.NameContains.Trim();
                query = query.Where(d => d.Name.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(filter.Location))
            {
                var location = filter.Location.Trim();
                query = query.Where(d => d.Location == location);
            }

            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                var type = filter.Type.Trim();
                query = query.Where(d => d.Type == type);
            }

            if (filter.IsOnline.HasValue)
            {
                query = query.Where(d => d.IsOnline == filter.IsOnline.Value);
            }

            if (filter.MinThresholdWatts.HasValue)
            {
                query = query.Where(d =>
                    d.ThresholdWatts >= filter.MinThresholdWatts.Value);
            }

            return query;
        }

        private static IQueryable<Device> ApplySorting(
            IQueryable<Device> query,
            DeviceFilter filter)
        {
            return (filter.SortBy, filter.SortOrder) switch
            {
                (DeviceSortBy.Name, SortOrder.Asc) => query.OrderBy(d => d.Name),
                (DeviceSortBy.Name, SortOrder.Desc) => query.OrderByDescending(d => d.Name),

                (DeviceSortBy.Location, SortOrder.Asc) => query.OrderBy(d => d.Location),
                (DeviceSortBy.Location, SortOrder.Desc) => query.OrderByDescending(d => d.Location),

                (DeviceSortBy.Type, SortOrder.Asc) => query.OrderBy(d => d.Type),
                (DeviceSortBy.Type, SortOrder.Desc) => query.OrderByDescending(d => d.Type),

                (DeviceSortBy.IsOnline, SortOrder.Asc) => query.OrderBy(d => d.IsOnline),
                (DeviceSortBy.IsOnline, SortOrder.Desc) => query.OrderByDescending(d => d.IsOnline),

                (DeviceSortBy.RegisteredAt, SortOrder.Asc) => query.OrderBy(d => d.RegisteredAt),
                (DeviceSortBy.RegisteredAt, SortOrder.Desc) => query.OrderByDescending(d => d.RegisteredAt),

                _ => query.OrderByDescending(d => d.RegisteredAt)
            };
        }

        // ============================================================
        //   BASIC CRUD
        // ============================================================
        public Task<Device?> GetByIdAsync(Guid id)
        {
            return _context.Devices.FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task AddAsync(Device device)
        {
            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Device device)
        {
            _context.Devices.Update(device);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device is null)
                return;

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
        }
    }
}

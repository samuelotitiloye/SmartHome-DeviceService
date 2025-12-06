using DeviceService.Application.Interfaces;
using DeviceService.Application.Common.Models;
using DeviceService.Application.Devices.Models;
using DeviceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DeviceService.Domain.Entities;

namespace DeviceService.Infrastructure.Repositories
{
    /// <summary>
    /// Provides data access operations for <see cref="Device"/> entities,
    /// including filtering, sorting, pagination, and basic CRUD functionality.
    /// This repository centralizes EF Core interactions and ensures consistent patterns
    /// across services in the SmartHome platform.
    /// </summary>
    public class DeviceRepository : IDeviceRepository
    {
        private readonly DeviceDbContext _context;

        /// <summary>
        /// Initializes a new <see cref="DeviceRepository"/> using the given database context.
        /// </summary>
        public DeviceRepository(DeviceDbContext context)
        {
            _context = context;
        }

        // ======================================================================
        //   PAGINATED QUERY
        // ======================================================================

        /// <summary>
        /// Retrieves a paginated list of devices matching the provided filter and sorting rules.
        /// Pagination is normalized to valid ranges and executed fully server-side.
        /// </summary>
        /// <param name="filter">Filtering and sorting criteria.</param>
        /// <param name="pagination">Pagination parameters including page number and page size.</param>
        /// <param name="cancellationToken">Token to cancel asynchronous operation.</param>
        /// <returns>A <see cref="PaginatedResult{T}"/> containing matching devices.</returns>
        public async Task<PaginatedResult<Device>> GetDevicesAsync(
            DeviceFilter filter,
            PaginationParameters pagination,
            CancellationToken cancellationToken = default)
        {
            filter.Normalize();
            var query = BuildFilteredQuery(filter);

            var totalCount = await query.CountAsync(cancellationToken);

            if (totalCount == 0)
                return PaginatedResult<Device>.Empty(pagination);

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

        // ======================================================================
        //   COUNT QUERY
        // ======================================================================

        /// <summary>
        /// Returns the total number of devices matching the provided filter.
        /// The count is always performed server-side without loading entities.
        /// </summary>
        public Task<int> GetDevicesCountAsync(
            DeviceFilter filter,
            CancellationToken cancellationToken = default)
        {
            filter.Normalize();
            return BuildFilteredQuery(filter).CountAsync(cancellationToken);
        }

        // ======================================================================
        //   QUERY BUILDING HELPERS
        // ======================================================================

        /// <summary>
        /// Builds an EF Core query containing all filtering rules based on <see cref="DeviceFilter"/>.
        /// This method does not execute the query; it prepares an <see cref="IQueryable{T}"/>
        /// that can later be counted, paginated, or executed.
        /// </summary>
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
                query = query.Where(d => d.ThresholdWatts >= filter.MinThresholdWatts.Value);
            }

            return query;
        }

        /// <summary>
        /// Applies a consistent sorting rule to an existing filtered device query.
        /// Sorting is performed strictly server-side using EF Core translation.
        /// </summary>
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

        // ======================================================================
        //   CRUD OPERATIONS
        // ======================================================================

        /// <summary>
        /// Retrieves a device by its identifier, or null if not found.
        /// </summary>
        public Task<Device?> GetByIdAsync(Guid id)
        {
            return _context.Devices.FirstOrDefaultAsync(d => d.Id == id);
        }

        /// <summary>
        /// Adds a new device to the database and saves changes.
        /// </summary>
        public async Task AddAsync(Device device)
        {
            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates an existing device entity and persists the change to the database.
        /// </summary>
        public async Task UpdateAsync(Device device)
        {
            _context.Devices.Update(device);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a device by its identifier if it exists.
        /// </summary>
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

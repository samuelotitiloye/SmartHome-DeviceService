using DeviceService.Application.Interfaces;
using DeviceService.Domain.Entities;
using DeviceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DeviceService.Application.Devices.Queries;   
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

        public async Task<Device?> GetByIdAsync(Guid id)
        {
            return await _context.Devices.FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IReadOnlyList<Device>> GetAllAsync()
        {
            return await _context.Devices.AsNoTracking().ToListAsync();
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
            if (device is null) return;

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<Device>> GetPagedAsync(    // filtering & pagination support
            int page,
            int pageSize,
            string? type,
            string? location,
            bool? isOnline)
        {
            var query = _context.Devices.AsQueryable();
            
            if(!string.IsNullOrWhiteSpace(type))
                query = query.Where(d => d.Type.ToLower() == type.ToLower());

            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(d => d.Location.ToLower() == location.ToLower());

            if (isOnline.HasValue)
                query = query.Where(d => d.IsOnline == isOnline.Value);

            var totalItems = await query.CountAsync();

            var items = await query 
                .OrderBy(d => d.Name)   //default
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Device>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }
    }
}

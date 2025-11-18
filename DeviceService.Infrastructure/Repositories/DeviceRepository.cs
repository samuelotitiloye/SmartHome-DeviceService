using DeviceService.Application.Interfaces;
using DeviceService.Domain.Entities;
using DeviceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
    }
}

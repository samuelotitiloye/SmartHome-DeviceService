using DeviceService.Domain.Entities;

namespace DeviceService.Application.Interfaces;
    public interface IDeviceRepository
    {
        Task AddAsync(Device device, CancellationToken ct = default);
        Task<Device?> GetByIdAsync(Guid id, CancellationToken ct = default);
    }    

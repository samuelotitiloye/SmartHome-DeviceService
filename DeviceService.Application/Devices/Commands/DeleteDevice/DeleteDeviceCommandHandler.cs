using DeviceService.Domain.Repositories;
using MediatR;

namespace DeviceService.Application.Devices.Commands.DeleteDevice
{
    public class DeleteDeviceCommandHandler : IRequestHandler<DeleteDeviceCommand, bool>
    {
        private readonly IDeviceRepository _repo;

        public DeleteDeviceCommandHandler(IDeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task<bool> Handle(DeleteDeviceCommand cmd, CancellationToken ct)
        {
            return await _repo.DeleteAsync(cmd.Id);
        }
    }
}

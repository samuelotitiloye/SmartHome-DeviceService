using DeviceService.Application.Interfaces;
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

        public async Task<bool> Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
        {
            var existing = await _repo.GetByIdAsync(request.Id);
            if (existing == null)
                return false;

            await _repo.DeleteAsync(request.Id);
            return true;
        }
    }
}

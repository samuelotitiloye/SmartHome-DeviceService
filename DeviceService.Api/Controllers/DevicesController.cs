using DeviceService.Application.Dto;
using DeviceService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeviceService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly DevicesService _deviceService;

        public DevicesController(DevicesService deviceService)
        {
            _deviceService = deviceService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceDto dto, CancellationToken ct)
        {
            var device = await _deviceService.RegisterDeviceAsync(dto, ct);
            return CreatedAtAction(nameof(GetDeviceById), new { id = device.Id }, device);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDeviceById(Guid id, CancellationToken ct)
        {
            // Implementation for retrieving a device by ID would go here properly. For now, just return OK.
            return Ok(new {message = "GetById not implemented yet."});
        }
    }
}
using DeviceService.Application.Dto;
using DeviceService.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DeviceService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly DevicesService _service;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(DevicesService service, ILogger<DevicesController> logger)
        {
            _service = service;
            _logger = logger;
        }
        /// <summary>
        /// Registers a new smart home device.
        /// </summary>
        /// <param name="dto">The device registration details.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The newly created device.</returns>
        [Tags("Devices")]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceDto dto, CancellationToken ct)
        {
            _logger.LogInformation("RegisterDevice called with DeviceName={DeviceName}, Type={Type}", 
            dto.DeviceName, dto.Type);

            try
            {
                var deviceDto = await _service.RegisterDeviceAsync(dto, ct);
                _logger.LogInformation("Device registered successfully with Id={Id}", deviceDto.Id);
                return CreatedAtAction(nameof(GetDeviceById), new { id = deviceDto.Id }, deviceDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Validation failed while registering device: {Message}", ex.Message);
                return BadRequest(new {error = ex.Message });
            }

        }
        /// <summary>
        /// Retrieves a device by its unique ID.
        /// </summary>
        /// <param name="id">Device ID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The matching device, or 404 if not found.</returns> 
        [Tags("Devices")]   
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDeviceById(Guid id, CancellationToken ct)
        {
            _logger.LogInformation("GetDeviceById called with Id={Id}", id);

            var device = await _service.GetByIdAsync(id, ct);

            if (device == null)
            {
                _logger.LogWarning("Device with Id={Id} not found", id);
                return NotFound();
            }

            _logger.LogInformation("Device found with Id={Id}", id);
            return Ok(device);
        }

        /// <summary>
        /// Retrieves all registered devices.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A list of all devices.</returns>
        [Tags("Devices")]
        [HttpGet]
        public async Task<IActionResult> GetAllDevices(CancellationToken ct)
        {
            _logger.LogInformation("GetAllDevices called");

            var devices = await _service.GetAllAsync(ct);
            _logger.LogInformation("Returning {Count} devices", devices.Count());

            return Ok(devices);
        }
    }
}
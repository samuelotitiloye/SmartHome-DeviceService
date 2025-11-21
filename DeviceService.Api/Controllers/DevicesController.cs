using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Services;
using DeviceService.Application.Devices.Commands.UpdateDevice;
using DeviceService.Application.Devices.Commands.RegisterDevice;
using DeviceService.Application.Devices.Commands.DeleteDevice;

namespace DeviceService.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Devices")]
    public class DevicesController : ControllerBase
    {
        private readonly DevicesService _service;
        private readonly ILogger<DevicesController> _logger;
        private readonly IMediator _mediator;

        public DevicesController(
            DevicesService service,
            ILogger<DevicesController> logger,
            IMediator mediator)
        {
            _service = service;
            _logger = logger;
            _mediator = mediator;
        }

        // -----------------------
        //  REGISTER DEVICE
        // -----------------------

          /// <summary>
        /// Registers a new smart home device.
        /// </summary>
        /// <param name="dto">The device information to register.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The created device with assigned ID.</returns>
        /// <response code="201">Device successfully created.</response>
        /// <response code="400">Invalid device data.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceDto dto, CancellationToken ct)
        {
            var result = await _service.RegisterDeviceAsync(dto, ct);
            return CreatedAtAction(nameof(GetDeviceById), new { id = result.Id }, result);
        }

        // -----------------------
        //  GET DEVICE BY ID
        // -----------------------
           /// <summary>
        /// Retrieves a device by its unique ID.
        /// </summary>
        /// <param name="id">Device ID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The device if found.</returns>
        /// <response code="200">Device returned.</response>
        /// <response code="404">Device not found.</response>
        [HttpGet("{id:guid}")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDeviceById(Guid id, CancellationToken ct)
        {
            var device = await _service.GetByIdAsync(id, ct);
            if (device is null)
                return NotFound();

            return Ok(device);
        }

        // -----------------------
        //  GET ALL DEVICES
        // -----------------------
        /// <summary>
        /// Retrieves all registered devices.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of devices.</returns>
        /// <response code="200">Devices returned.</response>
        [HttpGet]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
        [ProducesResponseType(typeof(IEnumerable<DeviceDto>), StatusCodes.Status200OK)]
         public async Task<IActionResult> GetAllDevices(CancellationToken ct)
        {
            var list = await _service.GetAllAsync(ct);
            return Ok(list);
        }

        // -----------------------
        //  UPDATE DEVICE
        // -----------------------
        /// <summary>
        /// Updates an existing device.
        /// </summary>
        /// <param name="id">Device ID.</param>
        /// <param name="dto">Updated device fields.</param>
        /// <returns>The updated device.</returns>
        /// <response code="200">Device updated successfully.</response>
        /// <response code="404">Device not found.</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDevice(Guid id, [FromBody] UpdateDeviceDto dto)
        {
            var result = await _mediator.Send(new UpdateDeviceCommand(
                id,
                dto.Name,
                dto.Type,
                dto.Location,
                dto.IsOnline,
                dto.ThresholdWatts,
                dto.SerialNumber
            ));

            if (result is null)
                return NotFound();

            return Ok(result);
        }

        // -----------------------
        //  DELETE DEVICE
        // -----------------------
        /// <summary>
        /// Deletes a device by ID.
        /// </summary>
        /// <param name="id">Device ID.</param>
        /// <returns>No content if deletion is successful.</returns>
        /// <response code="204">Device deleted.</response>
        /// <response code="404">Device not found.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
         public async Task<IActionResult> DeleteDevice(Guid id)
        {
            var deleted = await _mediator.Send(new DeleteDeviceCommand(id));

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}

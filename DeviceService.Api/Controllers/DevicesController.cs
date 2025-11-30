using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Services;
using DeviceService.Application.Devices.Commands.UpdateDevice;
using DeviceService.Application.Devices.Commands.RegisterDevice;
using DeviceService.Application.Devices.Commands.DeleteDevice;
using DeviceService.Api.Models;
using DeviceService.Application.Common.Models;
using DeviceService.Application.Devices.Models;
using DeviceService.Application.Mappings;
using Swashbuckle.AspNetCore.Annotations;
using DeviceService.Application.Interfaces;



namespace DeviceService.Api.Controllers
{   
    /// <summary>
    /// Manages all operations for SmartHome devices (creation, update, retrieval and deletion)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Tags("Devices")]
    public class DevicesController : ControllerBase
    {
        private readonly IDevicesService _service;
        private readonly ILogger<DevicesController> _logger;
        private readonly IMediator _mediator;

        public DevicesController(
            IDevicesService service,
            ILogger<DevicesController> logger,
            IMediator mediator)
        {
            _service = service;
            _logger = logger;
            _mediator = mediator;
        }


        // ==============================================================
        //  REGISTER DEVICE
        // ==============================================================
        /// <summary>
        /// Registers a new SmartHome device in the system.
        /// </summary>
        /// <param name="dto">The device information required to create a new device.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The newly created device, including its assigned ID.</returns>
        [HttpPost("register")]
        [SwaggerOperation(
        Summary = "Registers a new SmartHome device",
        Description = "Creates a device record and returns the created device with its ID.")]
        [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceDto dto, CancellationToken ct)
        {
            var result = await _service.RegisterDeviceAsync(dto, ct);
            return CreatedAtAction(nameof(GetDeviceById), new { id = result.Id }, result);
        }


        /// ==============================================================
        //  GET DEVICE BY ID
        // ==============================================================
        /// <summary>
        /// Retrieves a single SmartHome device by its unique identifier.
        /// </summary>
        /// <param name="id">The unique device ID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The device if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDeviceById(Guid id, CancellationToken ct)
        {
            var device = await _service.GetByIdAsync(id, ct);
            if (device is null)
                return NotFound();

            return Ok(device);
        }


        /// ==============================================================
        //  GET DEVICES (PAGINATED)
        // ==============================================================
        /// <summary>
        /// Retrieves a paginated list of SmartHome devices with optional filtering and sorting.
        /// </summary>
        /// <param name="query">Pagination, filtering, and sorting parameters.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A paginated collection of devices with metadata.</returns>

        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<DeviceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllDevicesAsync([FromQuery] GetDeviceQuery query, CancellationToken ct)
        {
            var pagination = new PaginationParameters(query.PageNumber, query.PageSize);

            var filter = new DeviceFilter
            {
                NameContains = query.Name,
                Location = query.Location,
                Type = query.Type,
                IsOnline = query.IsOnline,   // bool?
                SortBy = query.SortBy,
                SortOrder = query.SortOrder
            };

            var result = await _service.GetDevicesAsync(filter, pagination, ct);

            return Ok(result);
        }


        // ==============================================================
        //  UPDATE DEVICE
        // ==============================================================
        /// <summary>
        /// Updates an existing SmartHome device with new information.
        /// </summary>
        /// <param name="id">The ID of the device to update.</param>
        /// <param name="dto">The updated device values.</param>
        /// <returns>
        /// The updated device if the operation succeeds; otherwise, a 404 Not Found response.
        /// </returns>
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


        // ==============================================================
        //  DELETE DEVICE
        // ==============================================================
        /// <summary>
        /// Deletes a SmartHome device from the system.
        /// </summary>
        /// <param name="id">The ID of the device to delete.</param>
        /// <returns>
        /// A 204 No Content response if the device was deleted; otherwise, a 404 Not Found.
        /// </returns>
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

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


namespace DeviceService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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


        // ==============================================================
        //  REGISTER DEVICE
        // ==============================================================
        [HttpPost("register")]
        [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceDto dto, CancellationToken ct)
        {
            var result = await _service.RegisterDeviceAsync(dto, ct);
            return CreatedAtAction(nameof(GetDeviceById), new { id = result.Id }, result);
        }


        // ==============================================================
        //  GET DEVICE BY ID
        // ==============================================================
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


        // ==============================================================
        //  GET DEVICES (PAGINATED)
        // ==============================================================
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

            var result = await _service.GetPagedAsync(
                query.PageNumber ?? 1,
                query.PageSize ?? 10,
                query.Type,
                query.Location,
                query.IsOnline,
                ct
            );

            var dtoItems = result.Items.Select(d => d.ToDto()).ToList();

            var response = new PaginatedResult<DeviceDto>(
                dtoItems,
                result.PageNumber,
                result.PageSize,
                result.TotalCount
            );

            return Ok(response);
        }


        // ==============================================================
        //  UPDATE DEVICE
        // ==============================================================
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

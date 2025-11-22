using System.ComponentModel.DataAnnotations;
using DeviceService.Application.Devices.Models;

namespace DeviceService.Api.Models
{
    /// <summary>
    /// Represents query parameters for filtering, sorting, and paginating SmartHome devices.
    /// </summary>
    public class GetDeviceQuery
    {
        /// <summary>
        /// Page number of the result set (default is 1).
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per page (default is 10). Maximum allowed is 100.
        /// </summary>
        [Range(1, 100)]
        public int? PageSize { get; set; } = 10;

        /// <summary>
        /// Filters devices by partial name match.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Filters devices by physical installation location.
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Filters devices by type (e.g., Thermostat, Sensor).
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Filters devices by online/offline status.
        /// </summary>
        public bool? IsOnline { get; set; }

        /// <summary>
        /// Filters by minimum battery level percentage (0–100).
        /// </summary>
        [Range(0, 100)]
        public int? MinBatteryLevel { get; set; }

        /// <summary>
        /// Determines which field devices will be sorted by.
        /// </summary>
        public DeviceSortBy SortBy { get; set; } = DeviceSortBy.RegisteredAt;

        /// <summary>
        /// Determines the sorting direction (ascending or descending).
        /// </summary>
        public SortOrder SortOrder { get; set; } = SortOrder.Desc;
    }
}

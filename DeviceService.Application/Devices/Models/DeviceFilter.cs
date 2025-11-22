namespace DeviceService.Application.Devices.Models
{
    /// <summary>
    /// Specifies the field to sort SmartHome devices by.
    /// </summary>
    public enum DeviceSortBy
    {
        /// <summary>Sort by device registration date.</summary>
        RegisteredAt = 0,

        /// <summary>Sort by device name.</summary>
        Name = 1,

        /// <summary>Sort by device installation location.</summary>
        Location = 2,

        /// <summary>Sort by device type.</summary>
        Type = 3,

        /// <summary>Sort by online/offline status.</summary>
        IsOnline = 4
    }

    /// <summary>
    /// Represents the available sorting directions.
    /// </summary>
    public enum SortOrder
    {
        /// <summary>Sort in ascending order.</summary>
        Asc = 0,

        /// <summary>Sort in descending order.</summary>
        Desc = 1
    }

    /// <summary>
    /// Defines filtering and sorting options for retrieving SmartHome devices.
    /// </summary>
    public class DeviceFilter
    {
        /// <summary>
        /// Partial name filter for matching device names.
        /// </summary>
        public string? NameContains { get; set; }

        /// <summary>
        /// Filters devices by installation location.
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Filters devices by type (e.g., Thermostat, Camera, Sensor).
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Filters devices by online/offline status.
        /// </summary>
        public bool? IsOnline { get; set; }

        /// <summary>
        /// Filters devices by minimum configured watt threshold.
        /// </summary>
        public int? MinThresholdWatts { get; set; }

        /// <summary>
        /// Specifies which field results should be sorted by.
        /// </summary>
        public DeviceSortBy SortBy { get; set; } = DeviceSortBy.RegisteredAt;

        /// <summary>
        /// Specifies whether results should be sorted ascending or descending.
        /// </summary>
        public SortOrder SortOrder { get; set; } = SortOrder.Desc;
    }
}

namespace DeviceService.Application.Devices.Models
{
    /// <summary>
    /// Specifies the field to sort SmartHome devices by.
    /// </summary>
    public enum DeviceSortBy
    {
        RegisteredAt = 0,
        Name = 1,
        Location = 2,
        Type = 3,
        IsOnline = 4
    }

    /// <summary>
    /// Represents available sorting directions.
    /// </summary>
    public enum SortOrder
    {
        Asc = 0,
        Desc = 1
    }

    /// <summary>
    /// Defines filtering and sorting options for retrieving SmartHome devices.
    /// Contains normalization methods to enforce consistent behavior.
    /// </summary>
    public class DeviceFilter
    {
        public string? NameContains { get; set; }
        public string? Location { get; set; }
        public string? Type { get; set; }
        public bool? IsOnline { get; set; }
        public int? MinThresholdWatts { get; set; }

        public DeviceSortBy SortBy { get; set; } = DeviceSortBy.RegisteredAt;
        public SortOrder SortOrder { get; set; } = SortOrder.Desc;

        /// <summary>
        /// Normalizes all fields to ensure filtering behavior is consistent
        /// and avoids whitespace or invalid input issues.
        /// </summary>
        public void Normalize()
        {
            NameContains = NormalizeString(NameContains);
            Location = NormalizeString(Location);
            Type = NormalizeString(Type);

            // Defensive enum correction – required for API binding safety
            if (!Enum.IsDefined(typeof(DeviceSortBy), SortBy))
                SortBy = DeviceSortBy.RegisteredAt;

            if (!Enum.IsDefined(typeof(SortOrder), SortOrder))
                SortOrder = SortOrder.Desc;
        }

        private static string? NormalizeString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }
    }
}

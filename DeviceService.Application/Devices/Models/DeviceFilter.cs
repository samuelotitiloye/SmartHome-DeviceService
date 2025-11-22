namespace DeviceService.Application.Devices.Models
{
    public enum DeviceSortBy
    {
        RegisteredAt = 0,
        Name = 1,
        Location = 2,
        Type = 3,
        IsOnline = 4
    }

    public enum SortOrder
    {
        Asc = 0,
        Desc = 1
    }

    public class DeviceFilter
    {
        public string? NameContains { get; set; }
        public string? Location { get; set; }
        public string? Type { get; set; }
        public bool? IsOnline { get; set; }
        public int? MinThresholdWatts { get; set; }

        public DeviceSortBy SortBy { get; set; } = DeviceSortBy.RegisteredAt;
        public SortOrder SortOrder { get; set; } = SortOrder.Desc;
    }
}

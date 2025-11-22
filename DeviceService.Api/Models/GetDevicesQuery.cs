using System.ComponentModel.DataAnnotations;
using DeviceService.Application.Devices.Models;

namespace DeviceService.Api.Models
{
    public class GetDeviceQuery
    {
        [Range(1, int.MaxValue)]
        public int? PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int? PageSize { get; set; } = 10;

        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? Type { get; set; }
        public bool? IsOnline { get; set; }

        //filter
        [Range(0, 100)]
        public int? MinBatteryLevel { get; set; }

        //sorting
        public DeviceSortBy SortBy { get; set; } = DeviceSortBy.RegisteredAt;
        public SortOrder SortOrder { get; set; } = SortOrder.Desc;
    }
}
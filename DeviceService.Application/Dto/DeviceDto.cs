namespace DeviceService.Application.Dto
{
    public class DeviceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string Location { get; set; } = default!;
        public bool IsOnline { get; set; }
        public int ThresholdWatts { get; set; }
        public string SerialNumber { get; set; } = default!;
        public DateTime RegisteredAt { get; set; }
    }
}

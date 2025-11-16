namespace DeviceService.Application.Dto
{
    public class RegisterDeviceDto
    {
        public string DeviceName { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string Location { get; set; } = default!;
        public int ThresholdWatts { get; set; }
        public string SerialNumber { get; set; } = default!;
    }
}

namespace DeviceService.Application.Devices.Caching
{
    public static class DeviceCacheKeys
    {
        public static string DeviceById(Guid id) => $"device:{id}";
        public static string DeviceList => "device:list";
    }
}
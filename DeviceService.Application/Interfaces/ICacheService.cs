namespace DeviceService.Application.Interfaces
{
    public interface ICacheService
    {
        Task<T?>GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, int? ttMinutes = null);
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
    }
}
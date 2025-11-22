namespace DeviceService.Api.Models
{
    public class DeviceListResponse<TItem>
    {
        public IReadOnlyCollection<TItem> Items { get; set; } = Array.Empty<TItem>();

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }

        public PaginationLinks Links { get; set; } = new();
    }

    public class PaginationLinks
    {
        public string? Self { get; set; }
        public string? Next { get; set; }
        public string? Previous { get; set; }
    }
}
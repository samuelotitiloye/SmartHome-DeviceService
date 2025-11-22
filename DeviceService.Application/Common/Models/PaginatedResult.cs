namespace DeviceService.Application.Common.Models
{
    public class PaginatedResult<T>
    {
        public IReadOnlyCollection<T> Items { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages { get; }
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;

        public PaginatedResult(
            IReadOnlyCollection<T> items,
            int pageNumber,
            int pageSize,
            int totalCount)
        {
            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;

            TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        }

        public static PaginatedResult<T> Empty(PaginationParameters pagination) => 
            new PaginatedResult<T>(Array.Empty<T>(), pagination.PageNumber, pagination.PageSize, 0);
    }
}
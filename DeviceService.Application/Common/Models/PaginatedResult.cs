namespace DeviceService.Application.Common.Models
{
    /// <summary>
    /// Represents a paginated result set that contains items along with pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of items contained in the result.</typeparam>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// The items in the current page.
        /// </summary>
        public IReadOnlyCollection<T> Items { get; }

        /// <summary>
        /// The current page number.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// The number of items per page.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// The total number of items across all pages.
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// The total number of pages available.
        /// </summary>
        public int TotalPages { get; }

        /// <summary>
        /// Indicates whether a previous page exists.
        /// </summary>
        public bool HasPrevious => PageNumber > 1;

        /// <summary>
        /// Indicates whether a next page exists.
        /// </summary>
        public bool HasNext => PageNumber < TotalPages;

        /// <summary>
        /// Creates a new paginated result using the specified items and metadata.
        /// </summary>
        /// <param name="items">The items contained in the current page.</param>
        /// <param name="pageNumber">The current page number.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="totalCount">The total number of items across all pages.</param>
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

            TotalPages = pageSize == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize);
        }

        /// <summary>
        /// Creates an empty paginated result for the specified pagination settings.
        /// </summary>
        /// <param name="pagination">Pagination parameters to apply.</param>
        public static PaginatedResult<T> Empty(PaginationParameters pagination) =>
            new PaginatedResult<T>(
                Array.Empty<T>(),
                pagination.PageNumber,
                pagination.PageSize,
                0
            );
    }
}

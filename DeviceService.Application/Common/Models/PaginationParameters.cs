namespace DeviceService.Application.Common.Models
{
    /// <summary>
    /// Represents pagination settings including sanitized page number and page size values.
    /// </summary>
    public class PaginationParameters
    {
        private const int DefaultPageNumber = 1;
        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 100;

        /// <summary>
        /// The current page number after validation and normalization.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// The number of items per page after enforcing min/max limits.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Creates a new pagination configuration using safe defaults and validated values.
        /// </summary>
        /// <param name="pageNumber">Requested page number.</param>
        /// <param name="pageSize">Requested page size.</param>
        public PaginationParameters(int? pageNumber, int? pageSize)
        {
            var page = pageNumber.GetValueOrDefault(DefaultPageNumber);
            var size = pageSize.GetValueOrDefault(DefaultPageSize);

            if (page < 1)
                page = DefaultPageNumber;

            if (size < 1)
                size = DefaultPageSize;

            if (size > MaxPageSize)
                size = MaxPageSize;

            PageNumber = page;
            PageSize = size;
        }

        /// <summary>
        /// The number of items to skip when executing a paginated query.
        /// </summary>
        public int Skip => (PageNumber - 1) * PageSize;

        /// <summary>
        /// The number of items to return in the current page.
        /// </summary>
        public int Take => PageSize;
    }
}

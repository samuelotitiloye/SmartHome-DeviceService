namespace DeviceService.Application.Common.Models
{
    /// <summary>
    /// Represents pagination settings including validated and normalized
    /// page number and page size values.
    /// </summary>
    public class PaginationParameters
    {
        private const int DefaultPageNumber = 1;
        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 100;

        /// <summary>
        /// The sanitized and normalized page number.
        /// Guaranteed to be >= 1.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// The sanitized and normalized page size.
        /// Guaranteed to be between 1 and 100.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Creates a new pagination configuration using safe defaults.
        /// </summary>
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
        /// The number of items to skip when performing the paged query.
        /// </summary>
        public int Skip => (PageNumber - 1) * PageSize;

        /// <summary>
        /// The number of items to take for this page.
        /// </summary>
        public int Take => PageSize;

        /// <summary>
        /// Clamps a value within the specified minimum and maximum range.
        /// Provided for consistent pagination behavior across services.
        /// </summary>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}

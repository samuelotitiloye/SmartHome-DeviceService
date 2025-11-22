namespace DeviceService.Application.Common.Models
{
    public class PaginationParameters
    {
        private const int DefaultPageNumber = 1;
        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 100;

        public int PageNumber { get; }
        public int PageSize { get; }

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

        public int Skip => (PageNumber - 1) * PageSize;
        public int Take => PageSize;
    }
}

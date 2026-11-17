using LibraryMVC.Domain.Entities;

namespace LibraryMVC.WebMVC.Models
{
    public class BookPageViewModel
    {
        public const int PageSize = 10;

        public IReadOnlyList<Book> Books { get; init; } = [];

        public int Page { get; init; } = 1;

        public int TotalCount { get; init; }

        public int TotalPages => TotalCount == 0
            ? 1
            : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public int RangeFrom => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;

        public int RangeTo => Math.Min(Page * PageSize, TotalCount);

        public bool HasPrevious => Page > 1;

        public bool HasNext => Page < TotalPages;

        /// <summary>Номери кнопок. null — пропуск між далекими сторінками.</summary>
        public IReadOnlyList<int?> VisiblePages
        {
            get
            {
                if (TotalPages <= 7)
                {
                    return Enumerable.Range(1, TotalPages).Select(number => (int?)number).ToList();
                }

                var pages = new List<int?> { 1 };
                var start = Math.Max(2, Page - 1);
                var end = Math.Min(TotalPages - 1, Page + 1);
                if (start > 2)
                {
                    pages.Add(null);
                }

                for (var number = start; number <= end; number++)
                {
                    pages.Add(number);
                }

                if (end < TotalPages - 1)
                {
                    pages.Add(null);
                }

                pages.Add(TotalPages);
                return pages;
            }
        }
    }
}

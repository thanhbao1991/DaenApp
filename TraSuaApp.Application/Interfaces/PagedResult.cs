namespace TraSuaApp.Applicationn.Interfaces

{
    /// <summary>
    /// Kết quả phân trang tối giản, không phụ thuộc EF Core.
    /// Đặt ở Shared để Api/Application/Infrastructure/Wpf cùng dùng.
    /// </summary>
    public sealed class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int Total { get; }
        public int Page { get; }
        public int PageSize { get; }

        public int TotalPages => (int)Math.Ceiling((double)Total / Math.Max(1, PageSize));
        public bool HasNext => Page < TotalPages;
        public bool HasPrevious => Page > 1;

        public PagedResult(IReadOnlyList<T> items, int total, int page, int pageSize)
        {
            Items = items ?? Array.Empty<T>();
            Total = total < 0 ? 0 : total;
            Page = page < 1 ? 1 : page;
            PageSize = pageSize < 1 ? 1 : pageSize;
        }
    }
}
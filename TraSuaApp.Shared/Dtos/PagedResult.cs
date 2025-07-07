namespace TraSuaApp.Shared.Dtos
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalItems { get; set; }

        public PagedResult(List<T> items, int total)
        {
            Items = items;
            TotalItems = total;
        }
    }
}
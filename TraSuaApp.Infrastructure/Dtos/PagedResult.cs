using System.Text.Json.Serialization;

public class PagedResultDto<T>
{
    public List<T> Items { get; }
    public int TotalItems { get; }

    [JsonConstructor]
    public PagedResultDto(List<T> items, int totalItems)
    {
        Items = items;
        TotalItems = totalItems;
    }
}

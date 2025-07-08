namespace TraSuaApp.Domain.Entities;

public partial class Log
{
    public Guid Id { get; set; }

    public DateTime ThoiGian { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Method { get; set; }

    public string? Path { get; set; }

    public string? QueryString { get; set; }

    public int StatusCode { get; set; }

    public string? Ip { get; set; }

    public long? DurationMs { get; set; }

    public string? ExceptionMessage { get; set; }

    public string? RequestBodyShort { get; set; }

    public string? ResponseBodyShort { get; set; }

    public Guid? EntityId { get; set; }

    public string? BeforeData { get; set; }
    public string? AfterData { get; set; }
}

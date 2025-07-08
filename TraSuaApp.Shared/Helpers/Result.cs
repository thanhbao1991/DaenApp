namespace TraSuaApp.Shared.Helpers;

public class Result
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;

    // ✅ Thêm các trường cho middleware
    public string? BeforeData { get; set; }
    public string? AfterData { get; set; }
    public Guid? EntityId { get; set; }

    // ✅ Factory methods
    public static Result Success(string message) => new() { IsSuccess = true, Message = message };
    public static Result Failure(string message) => new() { IsSuccess = false, Message = message };

    // ✅ Fluent chaining
    public Result WithBefore(object? data)
    {
        BeforeData = data == null ? null : System.Text.Json.JsonSerializer.Serialize(data);
        return this;
    }

    public Result WithAfter(object? data)
    {
        AfterData = data == null ? null : System.Text.Json.JsonSerializer.Serialize(data);
        return this;
    }

    public Result WithId(Guid id)
    {
        EntityId = id;
        return this;
    }
}
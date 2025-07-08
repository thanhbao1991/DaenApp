using System.Text.Json;

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    // Cho middleware log
    public string? BeforeData { get; set; }
    public string? AfterData { get; set; }
    public Guid? EntityId { get; set; }

    // ✅ Factory methods
    public static Result<T> Success(string message, T data)
        => new() { IsSuccess = true, Message = message, Data = data };

    public static Result<T> Failure(string message)
        => new() { IsSuccess = false, Message = message };

    // ✅ Fluent chaining
    public Result<T> WithBefore(object? data)
    {
        BeforeData = data == null ? null : JsonSerializer.Serialize(data);
        return this;
    }

    public Result<T> WithAfter(object? data)
    {
        AfterData = data == null ? null : JsonSerializer.Serialize(data);
        return this;
    }

    public Result<T> WithId(Guid id)
    {
        EntityId = id;
        return this;
    }
}
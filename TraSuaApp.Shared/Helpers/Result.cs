namespace TraSuaApp.Shared.Helpers;
public class Result<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public T? Data { get; set; }
    public object? BeforeData { get; set; }
    public object? AfterData { get; set; }

    // ---------- Factory Methods ----------
    public static Result<T> Success(T data) =>
        new Result<T> { IsSuccess = true, Data = data };

    public static Result<T> Success(string message, T data) =>
        new Result<T> { IsSuccess = true, Message = message, Data = data };

    public static Result<T> Success(T data, string message) =>
     new Result<T> { IsSuccess = true, Message = message, Data = data };

    public static Result<T> Failure(string message) =>
        new Result<T> { IsSuccess = false, Message = message };



    // ---------- Fluent Chaining ----------
    public Result<T> WithId(Guid id)
    {
        EntityId = id;
        return this;
    }

    public Result<T> WithBefore(object? before)
    {
        BeforeData = before;
        return this;
    }

    public Result<T> WithAfter(object? after)
    {
        AfterData = after;
        return this;
    }
}
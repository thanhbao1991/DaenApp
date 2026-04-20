namespace TraSuaApp.Infrastructure.Helpers;
public class Result<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    // ---------- Factory Methods ----------
    public static Result<T> Success(T data) =>
        new Result<T> { IsSuccess = true, Data = data };

    public static Result<T> Success(string message, T data) =>
        new Result<T> { IsSuccess = true, Message = message, Data = data };

    public static Result<T> Success(T data, string message) =>
     new Result<T> { IsSuccess = true, Message = message, Data = data };

    public static Result<T> Failure(string message) =>
        new Result<T> { IsSuccess = false, Message = message };





}

namespace TraSuaApp.Shared.Helpers;

public class Result
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;

    public static Result Success(string message)
        => new Result { IsSuccess = true, Message = message };

    public static Result Failure(string message)
        => new Result { IsSuccess = false, Message = message };
}
namespace TraSuaApp.Shared.Helpers;

public class Result
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;

    // ✅ Dữ liệu phục vụ middleware ghi log
    public Guid? EntityId { get; set; }
    public object? BeforeData { get; set; }
    public object? AfterData { get; set; }

    // ✅ Một số hàm tiện ích
    public static Result Success(string message, Guid? entityId = null, object? before = null, object? after = null)
        => new() { IsSuccess = true, Message = message, EntityId = entityId, BeforeData = before, AfterData = after };

    public static Result Failure(string message)
        => new() { IsSuccess = false, Message = message };
}
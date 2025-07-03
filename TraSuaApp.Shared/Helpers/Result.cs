namespace TraSuaApp.Shared.Helpers
{
    public class Result
    {
        public bool ThanhCong { get; set; }
        public string Message { get; set; } = string.Empty;

        public static Result Success(string message = "Thành công")
        {
            return new Result { ThanhCong = true, Message = message };
        }

        public static Result Failure(string message = "Thất bại")
        {
            return new Result { ThanhCong = false, Message = message };
        }
    }
}
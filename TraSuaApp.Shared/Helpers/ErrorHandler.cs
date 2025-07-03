namespace TraSuaApp.Shared.Helpers
{
    public class ErrorHandler
    {
        public virtual void Handle(Exception ex, string context = "")
        {
            var message = $"Lỗi: {ex.Message}";
            if (!string.IsNullOrEmpty(context))
                message += $"\nNgữ cảnh: {context}";

            // TODO: Ghi log vào file, DB, hoặc gửi Discord
            Console.WriteLine(message);
        }
    }
}
namespace TraSuaApp.Shared.Helpers
{
    public class UIExceptionHelper
    {


        public virtual void Handle(Exception ex, string context = "")
        {
            var message = $"{ex.Message}";
            if (!string.IsNullOrEmpty(context))
                message = $"{context}\n{message}";

            // TODO: Ghi log vào file, DB, hoặc gửi Discord
            Console.WriteLine(message);
        }
    }
}
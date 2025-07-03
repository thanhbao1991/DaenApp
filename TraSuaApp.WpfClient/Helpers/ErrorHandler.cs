using System.Windows;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Helpers
{
    public class WpfErrorHandler : ErrorHandler
    {
        public override void Handle(Exception ex, string context = "")
        {
            base.Handle(ex, context); // Gọi xử lý ghi log ở lớp cha

            var message = string.IsNullOrEmpty(context)
                ? ex.Message
                : $"{context}\n\n{ex.Message}";

            MessageBox.Show(message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

            Clipboard.SetText(message);
        }
    }
}
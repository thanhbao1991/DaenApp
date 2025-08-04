using System.Windows;

namespace TraSuaApp.WpfClient.Helpers
{
    public static class ToastHelper
    {
        public static void Show(string message)
        {
            MessageBox.Show(message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            // Có thể thay bằng SnackBar từ MaterialDesignThemes nếu bạn đang dùng
        }
    }
}

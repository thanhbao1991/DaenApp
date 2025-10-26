using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.WpfClient.Helpers
{
    public class WpfErrorHandler
    {
        private readonly TextBlock? _errorTextBlock;

        public WpfErrorHandler(TextBlock? errorTextBlock = null)
        {
            _errorTextBlock = errorTextBlock;
        }

        public void Clear()
        {
            if (_errorTextBlock != null)
                _errorTextBlock.Text = string.Empty;
        }

        public void Handle(Exception ex, string context = "")
        {
            string message = ExtractMessage(ex);

            void Show()
            {
                if (_errorTextBlock != null)
                {
                    _errorTextBlock.Text = message;
                    _errorTextBlock.Foreground = Brushes.OrangeRed;
                }
                else
                {
                    MessageBox.Show(message,
                        string.IsNullOrWhiteSpace(context) ? "Lỗi" : context,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Bảo đảm chạy trên UI thread
            if (Application.Current?.Dispatcher?.CheckAccess() == true) Show();
            else Application.Current?.Dispatcher?.Invoke(Show);

            // Log lên Discord (không block UI)
            _ = DiscordService.SendAsync(Shared.Enums.DiscordEventType.Admin, ex.ToString());
        }

        private static string ExtractMessage(Exception ex)
        {
            try
            {
                var raw = ex.Message?.Trim() ?? "";
                if (string.IsNullOrEmpty(raw)) return "Đã xảy ra lỗi không xác định.";

                if (raw.StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(raw);
                    var root = doc.RootElement;

                    // ưu tiên "message", sau đó "error", sau đó toàn bộ JSON
                    if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                        return m.GetString() ?? "Đã xảy ra lỗi.";
                    if (root.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String)
                        return e.GetString() ?? "Đã xảy ra lỗi.";

                    return root.ToString();
                }

                return raw;
            }
            catch
            {
                return ex.Message ?? "Đã xảy ra lỗi.";
            }
        }
    }
}
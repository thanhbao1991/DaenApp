using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Helpers
{
    public class WpfErrorHandler : ErrorHandler
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
        public override void Handle(Exception ex, string context = "")
        {
            base.Handle(ex, context); // Ghi log ở lớp cha

            var message = string.IsNullOrEmpty(context)
                ? ex.Message
                : $"{ex.Message}";

            if (_errorTextBlock != null)
            {
                _errorTextBlock.Text = message;
            }
            else
            {
                MessageBox.Show(message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Clipboard.SetText(message);
        }
    }
}
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

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
            string message;
            if (ex.Message.TrimStart().StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(ex.Message);
                message = doc.RootElement.GetProperty("message").GetString() ?? string.Empty;
            }
            else
            {
                message = ex.Message;
            }


            if (_errorTextBlock != null)
            {
                _errorTextBlock.Text = message;
            }
            else
            {
                MessageBox.Show(message, context, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Clipboard.SetText(message);
        }
    }
}

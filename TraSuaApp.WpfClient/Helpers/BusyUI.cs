// Helpers/BusyUI.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TraSuaApp.WpfClient.Helpers
{
    public static class BusyUI
    {
        private sealed class Token : IDisposable
        {
            private readonly FrameworkElement _root;
            private readonly Button? _btn;
            private readonly object? _oldContent;
            private readonly object? _oldTag;

            public Token(FrameworkElement root, Button? btn, string? busyText)
            {
                _root = root;
                _btn = btn;
                _oldContent = _btn?.Content;
                _oldTag = _btn?.Tag;

                _root.IsEnabled = false;
                _root.Opacity = 0.25;
                if (_btn != null)
                {
                    _btn.Content = string.IsNullOrWhiteSpace(busyText) ? "Đang xử lý..." : busyText;
                    _btn.Tag = "loading";
                }
                Mouse.OverrideCursor = Cursors.Wait;
            }

            public void Dispose()
            {
                _root.IsEnabled = true;
                _root.Opacity = 1;
                if (_btn != null)
                {
                    _btn.Content = _oldContent;
                    _btn.Tag = _oldTag ?? "idle";
                }
                Mouse.OverrideCursor = null;
            }
        }

        public static IDisposable Scope(FrameworkElement root, Button? button = null, string? busyText = null)
            => new Token(root, button, busyText);
    }
}
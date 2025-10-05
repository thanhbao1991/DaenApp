using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TraSuaApp.WpfClient.Views
{
    // ==== Popup NotiWindow: giống LoadingWindow ====
    public class NotiWindow : Window
    {
        private readonly DispatcherTimer _messageTimer;
        private readonly DispatcherTimer _autoCloseTimer;
        private int _step = 0;
        private readonly string[] _messages =
        {
            "⚠️ Mất kết nối máy chủ...",
            "Đang thử kết nối lại.",
            "Đang thử kết nối lại..",
            "Đang thử kết nối lại...",
            "Đang chờ phản hồi...",
            "Kết nối vẫn chưa phục hồi..."
        };

        private readonly System.Windows.Controls.TextBlock _messageText;

        public NotiWindow()
        {
            // ===== Cấu hình cửa sổ =====
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SizeToContent = SizeToContent.WidthAndHeight;
            Opacity = 0;

            // ===== Lớp nền mờ + hộp trắng =====
            var overlay = new System.Windows.Controls.Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x55, 0x00, 0x00, 0x00)),
                Padding = new Thickness(0),
                Child = new System.Windows.Controls.Border
                {
                    Padding = new Thickness(24),
                    CornerRadius = new CornerRadius(10),
                    Background = Brushes.White,
                    Child = new System.Windows.Controls.StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Vertical,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Children =
                        {
                            // 🟟 Spinner xoay
                            new System.Windows.Shapes.Ellipse
                            {
                                Width = 40, Height = 40,
                                Stroke = Brushes.OrangeRed,
                                StrokeThickness = 5,
                                StrokeDashArray = new DoubleCollection { 2, 3 },
                                RenderTransform = new RotateTransform(0, 20, 20),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Margin = new Thickness(0, 0, 0, 12)
                            }
                        }
                    }
                }
            };

            Content = overlay;

            // ===== Spinner animation =====
            var panel = (System.Windows.Controls.StackPanel)
                ((System.Windows.Controls.Border)((System.Windows.Controls.Border)overlay).Child).Child;

            var spinner = (System.Windows.Shapes.Ellipse)panel.Children[0];
            var rotate = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(1)))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            ((RotateTransform)spinner.RenderTransform)
                .BeginAnimation(RotateTransform.AngleProperty, rotate);

            // ===== Text động =====
            _messageText = new System.Windows.Controls.TextBlock
            {
                Margin = new Thickness(12, 4, 12, 0),
                Text = _messages[0],
                FontSize = 16,
                Width = 220,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(_messageText);

            // ===== Fade in =====
            Loaded += (_, __) =>
            {
                var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)));
                BeginAnimation(OpacityProperty, fadeIn);
            };

            // ===== Đổi message động =====
            _messageTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.8) };
            _messageTimer.Tick += (_, __) =>
            {
                _step = (_step + 1) % _messages.Length;
                _messageText.Text = _messages[_step];
            };
            _messageTimer.Start();

            // ===== Tự đóng sau 30s =====
            _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _autoCloseTimer.Tick += (_, __) =>
            {
                _autoCloseTimer.Stop();
                Close();
            };
            _autoCloseTimer.Start();
        }

        // 🟟 Cập nhật message thủ công
        public void UpdateMessage(string msg)
        {
            Dispatcher.Invoke(() => _messageText.Text = msg);
        }

        // 🟟 Fade out khi đóng
        public new void Close()
        {
            _messageTimer.Stop();
            _autoCloseTimer.Stop();

            var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(300)));
            fadeOut.Completed += (_, __) => base.Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
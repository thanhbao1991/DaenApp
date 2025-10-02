using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TraSuaApp.WpfClient.Helpers
{
    // ==== Popup LoadingWindow (code-behind, không dùng XAML) ====
    public class LoadingWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private int _step = 0;
        private readonly string[] _messages =
        {
            "Đang phân tích ảnh...",
            "Đang nhận diện món...",
            "Đang tạo hoá đơn...",
            "Sắp xong rồi..."
        };

        private readonly System.Windows.Controls.TextBlock _messageText;

        public LoadingWindow(string message = "Đang xử lý...")
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
            ShowInTaskbar = false;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen; // => giữa màn hình
            Opacity = 0;
            ResizeMode = ResizeMode.NoResize;
            var overlay = new System.Windows.Controls.Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x66, 0x00, 0x00, 0x00)),
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(0),
                Child = new System.Windows.Controls.Border
                {
                    Padding = new Thickness(24),
                    CornerRadius = new CornerRadius(10),
                    Background = System.Windows.Media.Brushes.White,
                    Child = new System.Windows.Controls.StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Children =
                        {
                            // Spinner
                            new System.Windows.Shapes.Ellipse
                            {
                                Width = 40, Height = 40,
                                Stroke = System.Windows.Media.Brushes.DodgerBlue,
                                StrokeThickness = 5,
                                StrokeDashArray = new System.Windows.Media.DoubleCollection { 2, 3 },
                                RenderTransform = new System.Windows.Media.RotateTransform(0, 20, 20)
                            }
                        }
                    }
                }
            };

            Content = overlay;

            // Lấy spinner đã add ở trên và animate xoay
            var panel = (System.Windows.Controls.StackPanel)((System.Windows.Controls.Border)((System.Windows.Controls.Border)overlay).Child).Child;
            var spinner = (System.Windows.Shapes.Ellipse)panel.Children[0];
            var rotate = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(1)))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            ((System.Windows.Media.RotateTransform)spinner.RenderTransform)
                .BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, rotate);

            // Message text
            _messageText = new System.Windows.Controls.TextBlock
            {
                Margin = new Thickness(12, 16, 12, 0),
                Text = message,
                FontSize = 16,
                Width = 200,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.Black,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            panel.Children.Add(_messageText);

            // Fade in
            Loaded += (_, __) =>
            {
                var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)));
                BeginAnimation(OpacityProperty, fadeIn);
            };

            // Fake tiến trình: đổi message theo thời gian
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += (_, __) =>
            {
                if (_step < _messages.Length)
                {
                    _messageText.Text = _messages[_step];
                    _step++;
                }
            };
            _timer.Start();
        }

        public void UpdateMessage(string msg) =>
            Dispatcher.Invoke(() => _messageText.Text = msg);

        // Đóng có fade out
        public new void Close()
        {
            _timer.Stop();
            var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(300)));
            fadeOut.Completed += (_, __) => base.Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }

    // ==== Helper: chạy async kèm loading ====
    public static class AIOrderHelper
    {
        /// <summary>
        /// Chạy một hàm async kèm LoadingWindow giữa màn hình.
        /// Truyền owner nếu muốn "dính" theo cửa sổ cha; nếu không, để null cho an toàn.
        /// </summary>
        public static async Task<T> RunWithLoadingAsync<T>(string initialMessage, Func<Task<T>> func, Window? owner = null)
        {
            LoadingWindow? loading = null;
            try
            {
                loading = new LoadingWindow(initialMessage);

                // 🟟 CHỈ gán Owner nếu hợp lệ, tuyệt đối không đụng Application.Current.MainWindow ở đây
                if (owner != null && owner != loading)
                {
                    loading.Owner = owner;
                    loading.WindowStartupLocation = WindowStartupLocation.CenterOwner; // nếu có owner thì canh giữa owner
                }

                loading.Show();

                var result = await func();
                return result;
            }
            finally
            {
                loading?.Close();
            }
        }

        public static async Task RunWithLoadingAsync(string initialMessage, Func<Task> func, Window? owner = null)
        {
            LoadingWindow? loading = null;
            try
            {
                loading = new LoadingWindow(initialMessage);

                if (owner != null && owner != loading)
                {
                    loading.Owner = owner;
                    loading.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }

                loading.Show();

                await func();
            }
            finally
            {
                loading?.Close();
            }
        }
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TraSuaApp.WpfClient.Helpers
{
    public static class BusyUI
    {
        // 🟟 Spinner thuần C#
        private static FrameworkElement CreateSpinner(double size = 16, double strokeThickness = 2.0, double durationSeconds = 1.0)
        {
            var track = new Ellipse
            {
                Width = size,
                Height = size,
                StrokeThickness = strokeThickness,
                Stroke = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0))
            };

            var radius = size / 2;
            var startPoint = new Point(radius, 0);
            var endPoint = new Point(size, radius);

            var arc = new Path
            {
                Stroke = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0)),
                StrokeThickness = strokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Data = new PathGeometry(new[]
                {
                    new PathFigure
                    {
                        StartPoint = startPoint,
                        Segments = new PathSegmentCollection
                        {
                            new ArcSegment
                            {
                                Point = endPoint,
                                Size = new Size(radius, radius),
                                IsLargeArc = false,
                                SweepDirection = SweepDirection.Clockwise
                            }
                        }
                    }
                })
            };

            var spinner = new Grid
            {
                Width = size,
                Height = size,
                RenderTransformOrigin = new Point(0.5, 0.5),
                Children = { track, arc },
                RenderTransform = new RotateTransform(0)
            };

            var rotate = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseInOut }
            };

            spinner.Loaded += (_, __) =>
            {
                (spinner.RenderTransform as RotateTransform)?.BeginAnimation(RotateTransform.AngleProperty, rotate);
            };
            spinner.Unloaded += (_, __) =>
            {
                (spinner.RenderTransform as RotateTransform)?.BeginAnimation(RotateTransform.AngleProperty, null);
            };

            return new Viewbox { Width = size, Height = size, Stretch = Stretch.Uniform, Child = spinner };
        }

        // 🟟 Token điều khiển trạng thái bận
        private sealed class Token : IDisposable
        {
            private readonly FrameworkElement _root;
            private readonly Button? _btn;
            private readonly object? _oldContent;
            private readonly object? _oldTag;
            private readonly DispatcherTimer? _timer;
            private readonly string[] _messages = new[]
            {
                "Đang xử lý...",
                "Đang tính toán...",
                "Đang gửi dữ liệu...",
                "Đang chuẩn bị...",
                "Đang học hỏi..."
            };
            private int _index = 0;

            public Token(FrameworkElement root, Button? btn, string? busyText)
            {
                _root = root;
                _btn = btn;
                _oldContent = _btn?.Content;
                _oldTag = _btn?.Tag;

                if (_btn != null)
                {
                    // ❌ Không dùng IsEnabled=false (vì sẽ bị mờ)
                    // Thay bằng overlay bắt chuột, chặn click
                    _btn.IsHitTestVisible = false;

                    // Layout spinner + text
                    var sp = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    var spinner = CreateSpinner(size: 16, strokeThickness: 2.0, durationSeconds: 0.9);
                    var txt = new TextBlock
                    {
                        Text = busyText ?? "Đang xử lý...",
                        Margin = new Thickness(8, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    sp.Children.Add(spinner);
                    sp.Children.Add(txt);

                    _btn.Content = sp;
                    _btn.Tag = "loading";

                    // ⏱ Chuỗi "ảo" thay đổi mỗi 1.5 giây
                    _timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1.5)
                    };
                    _timer.Tick += (_, __) =>
                    {
                        _index = (_index + 1) % _messages.Length;
                        txt.Text = _messages[_index];
                    };
                    _timer.Start();
                }

                Mouse.OverrideCursor = Cursors.AppStarting;
            }

            public void Dispose()
            {
                if (_btn != null)
                {
                    _timer?.Stop();
                    _btn.Content = _oldContent;
                    _btn.Tag = _oldTag ?? "idle";
                    _btn.IsHitTestVisible = true; // bật lại click
                }
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Dùng: using (BusyUI.Scope(this, btnSave, "Đang lưu...")) { await ... }
        /// </summary>
        public static IDisposable Scope(FrameworkElement root, Button? button = null, string? busyText = "Đang xử lý...")
            => new Token(root, button, busyText);
    }
}
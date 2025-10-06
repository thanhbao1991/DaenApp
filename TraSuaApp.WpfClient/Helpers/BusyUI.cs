using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TraSuaApp.WpfClient.Helpers
{
    public static class BusyUI
    {
        /// <summary>
        /// Tạo spinner (vòng cung quay) thuần C# để nhét vào nút.
        /// </summary>
        private static FrameworkElement CreateSpinner(double size = 16, double strokeThickness = 2.0, double durationSeconds = 1.0)
        {
            // Vòng nền nhạt
            var track = new Ellipse
            {
                Width = size,
                Height = size,
                StrokeThickness = strokeThickness,
                Stroke = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), // nhạt
            };

            // Cung quay (1/3 vòng)
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
                        },
                        IsClosed = false
                    }
                })
            };

            // Container có RotateTransform để quay cả cung
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

            // Bắt đầu animation
            spinner.Loaded += (_, __) =>
            {
                (spinner.RenderTransform as RotateTransform)?.BeginAnimation(RotateTransform.AngleProperty, rotate);
            };

            // Dừng nếu bị unload (phòng ngừa leak)
            spinner.Unloaded += (_, __) =>
            {
                (spinner.RenderTransform as RotateTransform)?.BeginAnimation(RotateTransform.AngleProperty, null);
            };

            // Co giãn theo nút: bọc Viewbox để auto-scale
            return new Viewbox
            {
                Stretch = Stretch.Uniform,
                Width = size,
                Height = size,
                Child = spinner
            };
        }

        /// <summary>
        /// Token điều khiển trạng thái "bận" của một thao tác UI.
        /// </summary>
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

                // ❗ Chỉ khoá NÚT, không khoá toàn UI
                if (_btn != null)
                {
                    _btn.IsEnabled = false;

                    // Layout nút: [spinner] [text]
                    var sp = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    sp.Children.Add(CreateSpinner(size: 16, strokeThickness: 2.0, durationSeconds: 0.9));
                    sp.Children.Add(new TextBlock
                    {
                        Text = string.IsNullOrWhiteSpace(busyText) ? "Đang xử lý..." : busyText,
                        Margin = new Thickness(8, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    _btn.Content = sp;
                    _btn.Tag = "loading";
                }

                // Con trỏ nhẹ nhàng (không gây cảm giác đơ)
                Mouse.OverrideCursor = Cursors.AppStarting;
            }

            public void Dispose()
            {
                if (_btn != null)
                {
                    _btn.Content = _oldContent;
                    _btn.Tag = _oldTag ?? "idle";
                    _btn.IsEnabled = true;
                }

                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Dùng: using (BusyUI.Scope(this, button, "Đang phân tích…")) { await ... }
        /// </summary>
        public static IDisposable Scope(FrameworkElement root, Button? button = null, string? busyText = "Vui lòng chờ...")
            => new Token(root, button, busyText);
    }
}
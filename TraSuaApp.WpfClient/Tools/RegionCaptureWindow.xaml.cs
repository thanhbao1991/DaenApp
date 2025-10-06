using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TraSuaApp.WpfClient.Controls
{
    public partial class RegionCaptureWindow : Window
    {
        private bool _dragging = false;
        private Point _start;
        public Rect? SelectedRectDip { get; private set; }

        public RegionCaptureWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => { Activate(); Focus(); Keyboard.Focus(this); };

            KeyDown += RegionCaptureWindow_KeyDown;
            Layer.MouseDown += Layer_MouseDown;
            Layer.MouseMove += Layer_MouseMove;
            Layer.MouseUp += Layer_MouseUp;
        }

        private void RegionCaptureWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void Layer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragging = true;
            _start = e.GetPosition(Layer);

            SelectionRect.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRect, _start.X);
            Canvas.SetTop(SelectionRect, _start.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;

            // Quan trọng: capture trên Layer
            Layer.CaptureMouse();
        }

        private void Layer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            var pos = e.GetPosition(Layer);

            double x = Math.Min(pos.X, _start.X);
            double y = Math.Min(pos.Y, _start.Y);
            double w = Math.Abs(pos.X - _start.X);
            double h = Math.Abs(pos.Y - _start.Y);

            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width = w;
            SelectionRect.Height = h;
        }

        private void Layer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging) return;
            _dragging = false;

            // Nhả đúng đối tượng đã capture
            Layer.ReleaseMouseCapture();

            double x = Canvas.GetLeft(SelectionRect);
            double y = Canvas.GetTop(SelectionRect);
            double w = SelectionRect.Width;
            double h = SelectionRect.Height;

            if (w < 10 || h < 10)
            {
                DialogResult = false;
                Close();
                return;
            }

            SelectedRectDip = new Rect(x, y, w, h);
            DialogResult = true;
            Close();
        }
    }
}
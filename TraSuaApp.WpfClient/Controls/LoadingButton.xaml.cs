using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TraSuaApp.WpfClient.Controls
{
    public partial class LoadingButton : UserControl
    {
        private DispatcherTimer? _dotsTimer;
        private int _dotCount = 0;

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(string), typeof(LoadingButton), new PropertyMetadata("Lưu"));

        public string Content
        {
            get => (string)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(LoadingButton),
                new PropertyMetadata(false, OnIsBusyChanged));

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        // ✅ RoutedEvent Click cho UserControl
        public static readonly RoutedEvent ClickEvent =
            EventManager.RegisterRoutedEvent("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(LoadingButton));

        public event RoutedEventHandler Click
        {
            add { AddHandler(ClickEvent, value); }
            remove { RemoveHandler(ClickEvent, value); }
        }

        public LoadingButton()
        {
            InitializeComponent();
            MainButton.Click += (s, e) => RaiseEvent(new RoutedEventArgs(ClickEvent));
        }

        private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LoadingButton)d;
            bool isBusy = (bool)e.NewValue;

            if (isBusy)
            {
                control.StartDotsAnimation();
                control.StartBlinkAnimation();
            }
            else
            {
                control.StopDotsAnimation();
                control.StopBlinkAnimation();
                control.ButtonText.Text = control.Content;
            }
        }

        private void StartDotsAnimation()
        {
            _dotCount = 0;
            ButtonText.Text = "Đang lưu";
            _dotsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _dotsTimer.Tick += (s, e) =>
            {
                _dotCount = (_dotCount + 1) % 4;
                ButtonText.Text = "Đang lưu" + new string('.', _dotCount);
            };
            _dotsTimer.Start();
        }

        private void StopDotsAnimation()
        {
            _dotsTimer?.Stop();
            _dotsTimer = null;
        }

        private void StartBlinkAnimation()
        {
            if (FindResource("BlinkColorStoryboard") is Storyboard sb)
            {
                Storyboard.SetTarget(sb, ButtonText);
                sb.Begin();
            }
        }

        private void StopBlinkAnimation()
        {
            if (FindResource("BlinkColorStoryboard") is Storyboard sb)
            {
                sb.Stop();
            }
        }

    }
}
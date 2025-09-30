using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Controls
{
    public partial class NotiPanel : UserControl
    {
        // ========== Dependency Properties ==========
        public ObservableCollection<NotifyItem> ItemsSource
        {
            get => (ObservableCollection<NotifyItem>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource),
                typeof(ObservableCollection<NotifyItem>),
                typeof(NotiPanel),
                new PropertyMetadata(NotiCenter.Items)); // mặc định dùng NotiCenter

        public string HeaderTitle
        {
            get => (string)GetValue(HeaderTitleProperty);
            set => SetValue(HeaderTitleProperty, value);
        }
        public static readonly DependencyProperty HeaderTitleProperty =
            DependencyProperty.Register(nameof(HeaderTitle),
                typeof(string),
                typeof(NotiPanel),
                new PropertyMetadata("Thông báo"));

        public double CompactWidth
        {
            get => (double)GetValue(CompactWidthProperty);
            set => SetValue(CompactWidthProperty, value);
        }
        public static readonly DependencyProperty CompactWidthProperty =
            DependencyProperty.Register(nameof(CompactWidth),
                typeof(double),
                typeof(NotiPanel),
                new PropertyMetadata(320.0));

        public double CompactHeight
        {
            get => (double)GetValue(CompactHeightProperty);
            set => SetValue(CompactHeightProperty, value);
        }
        public static readonly DependencyProperty CompactHeightProperty =
            DependencyProperty.Register(nameof(CompactHeight),
                typeof(double),
                typeof(NotiPanel),
                new PropertyMetadata(140.0));

        public double ExpandedWidth
        {
            get => (double)GetValue(ExpandedWidthProperty);
            set => SetValue(ExpandedWidthProperty, value);
        }
        public static readonly DependencyProperty ExpandedWidthProperty =
            DependencyProperty.Register(nameof(ExpandedWidth),
                typeof(double),
                typeof(NotiPanel),
                new PropertyMetadata(520.0));

        public double ExpandedHeight
        {
            get => (double)GetValue(ExpandedHeightProperty);
            set => SetValue(ExpandedHeightProperty, value);
        }
        public static readonly DependencyProperty ExpandedHeightProperty =
            DependencyProperty.Register(nameof(ExpandedHeight),
                typeof(double),
                typeof(NotiPanel),
                new PropertyMetadata(480.0));
        // ===========================================

        public NotiPanel()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // đảm bảo kích thước ban đầu
            PART_Host.Width = CompactWidth;
            PART_Host.Height = CompactHeight;
        }

        private void Clear_Click(object sender, RoutedEventArgs e) => NotiCenter.Clear();

        private void Root_MouseEnter(object sender, MouseEventArgs e) => Animate(expand: true);
        private void Root_MouseLeave(object sender, MouseEventArgs e) => Animate(expand: false);

        private void Animate(bool expand)
        {
            if (PART_Host == null) return;

            double toW = expand ? ExpandedWidth : CompactWidth;
            double toH = expand ? ExpandedHeight : CompactHeight;

            Panel.SetZIndex(this, expand ? 9999 : 2);

            var dur = TimeSpan.FromMilliseconds(160);
            var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            PART_Host.BeginAnimation(FrameworkElement.WidthProperty,
                new DoubleAnimation { To = toW, Duration = dur, EasingFunction = ease });
            PART_Host.BeginAnimation(FrameworkElement.HeightProperty,
                new DoubleAnimation { To = toH, Duration = dur, EasingFunction = ease });

            if (PART_Host.Effect is DropShadowEffect dse)
            {
                dse.BeginAnimation(DropShadowEffect.BlurRadiusProperty,
                    new DoubleAnimation { To = expand ? 18 : 10, Duration = dur, EasingFunction = ease });
            }
        }
    }
}
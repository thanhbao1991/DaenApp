using System.Windows;
using System.Windows.Controls;

namespace TraSuaApp.WpfClient.Controls
{
    public partial class NumericUpDown : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(decimal),
                typeof(NumericUpDown),
                new FrameworkPropertyMetadata(
                    1m, // default: 1 (giữ hành vi cũ). Nếu muốn 0 thì đổi thành 0m.
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValuePropertyChanged));

        public decimal Value
        {
            get => (decimal)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;
            var newValue = (decimal)e.NewValue;

            // Đồng bộ từ DP Value -> txtValue.Value, tránh vòng lặp nếu đã giống nhau
            if (control.txtValue.Value != newValue)
            {
                control.txtValue.Value = newValue;
            }
        }

        public NumericUpDown()
        {
            InitializeComponent();

            // Khởi tạo txtValue theo Value mặc định (1m)
            txtValue.Value = Value;

            // Khi người dùng gõ / đổi trong NumericTextBox, sync ngược lại ra DependencyProperty Value
            txtValue.ValueChanged += (s, e) =>
            {
                if (Value != txtValue.Value)
                    Value = txtValue.Value;
            };
        }

        private void Minus_Click(object sender, RoutedEventArgs e)
        {
            if (Value > 0)
                Value -= 1;
        }

        private void Plus_Click(object sender, RoutedEventArgs e)
        {
            Value += 1;
        }
    }
}
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
                new FrameworkPropertyMetadata(1m, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public decimal Value
        {
            get => (decimal)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public NumericUpDown()
        {
            InitializeComponent();
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
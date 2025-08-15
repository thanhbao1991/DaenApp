using System.Windows;
using System.Windows.Controls;

namespace TraSuaApp.WpfClient.Controls
{
    public partial class NumericUpDown : UserControl
    {
        public NumericUpDown()
        {
            InitializeComponent();
        }

        private void Minus_Click(object sender, RoutedEventArgs e)
        {
            txtValue.Value = Math.Max(1, txtValue.Value - 1);
        }

        private void Plus_Click(object sender, RoutedEventArgs e)
        {
            txtValue.Value += 1;
        }

        public decimal Value
        {
            get => txtValue.Value;
            set => txtValue.Value = value;
        }
    }
}
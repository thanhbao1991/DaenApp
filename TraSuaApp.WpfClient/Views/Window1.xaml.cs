using System.Windows;
using System.Windows.Input;

namespace TraSuaApp.WpfClient.Views
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class ShipperDialog : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

        public ShipperDialog()
        {
            InitializeComponent();
        }

        private void Khanh_Click(object sender, MouseButtonEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            DialogResult = true;
        }

        private void Nha_Click(object sender, MouseButtonEventArgs e)
        {
            Result = MessageBoxResult.No;
            DialogResult = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Result = MessageBoxResult.Cancel;
                DialogResult = false;
            }

            base.OnKeyDown(e);
        }
    }
}
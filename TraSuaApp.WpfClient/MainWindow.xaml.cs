using System.Windows;
using TraSuaApp.WpfClient.Tools;

namespace TraSuaApp.WpfClient.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TaiKhoan_Click(object sender, RoutedEventArgs e)
        {
            new TaiKhoanList().ShowDialog();
            this.Close();
        }

        private void NhomSanPham_Click(object sender, RoutedEventArgs e)
        {
            new NhomSanPhamList().ShowDialog();
            this.Close();

        }

        private void SanPham_Click(object sender, RoutedEventArgs e)
        {
            new SanPhamList().ShowDialog();
            this.Close();

        }

        private void Thoat_Click(object sender, RoutedEventArgs e)
        {
            new CodeGeneratorWindow().ShowDialog();

            this.Close();
        }
    }
}
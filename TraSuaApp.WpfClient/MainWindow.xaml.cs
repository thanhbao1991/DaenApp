using System.Windows;
using System.Windows.Controls;

namespace TraSuaApp.WpfClient.Views
{
    public partial class MainWindow : Window
    {
        public string VaiTro { get; set; } = "NhanVien";
        public string UserId { get; set; } = "";
        public string TenHienThi { get; set; } = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (VaiTro != "Admin")
            {
                BtnTaiKhoan.Visibility = Visibility.Collapsed;
                //  BtnCodeGenerator.Visibility = Visibility.Collapsed;
                // Thêm các nút khác nếu cần phân quyền
            }

            // Ví dụ: hiển thị tên người dùng (nếu có TextBlock)
            // TenNguoiDungTextBlock.Text = $"Xin chào, {TenHienThi}";
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag) return;

            try
            {
                var namespaceName = "TraSuaApp.WpfClient.Views";
                var typeName = $"{namespaceName}.{tag}";
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    MessageBox.Show($"Không tìm thấy form: {tag}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (Activator.CreateInstance(type) is Window window)
                {
                    window.Owner = this;
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở form '{tag}': {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Thoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
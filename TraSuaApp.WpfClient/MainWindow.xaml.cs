using System.Windows;
using System.Windows.Controls;
using TraSuaApp.WpfClient.Tools;

namespace TraSuaApp.WpfClient.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag) return;

            try
            {
                // Tên namespace chứa các window
                var namespaceName = "TraSuaApp.WpfClient.Views";

                // Tạo full type name
                var typeName = $"{namespaceName}.{tag}";

                // Tìm type
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    MessageBox.Show($"Không tìm thấy form: {tag}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Tạo instance và ép kiểu thành Window
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
            new CodeGeneratorWindow().ShowDialog();

            this.Close();
        }
    }
}
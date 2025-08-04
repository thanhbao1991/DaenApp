using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Enums;

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
            GenerateMenuButtons();
        }


        private void GenerateMenuButtons()
        {
            var buttonsContainer = xxx; // StackPanel chứa nút
            var viewType = typeof(Window);
            var views = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(viewType) && t.Name.EndsWith("List"))
                .OrderBy(t => t.Name);

            foreach (var view in views)
            {
                var btn = new Button
                {
                    Content =
                    TuDien._tableFriendlyNames[view.Name.Replace("List", "")],

                    Tag = view.Name,
                    Style = (Style)FindResource("AddButtonStyle"),
                    Margin = new Thickness(0, 5, 0, 0)
                };
                btn.Click += MenuButton_Click;
                buttonsContainer.Children.Add(btn);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {


        }

        private async void MenuButton_Click(object sender, RoutedEventArgs e)
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

                // Hiển thị loading (nếu có custom loading dialog riêng)
                // LoadingOverlay.Show();

                await Task.Delay(100); // tạo cảm giác phản hồi nhanh hơn

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
            finally
            {
                // LoadingOverlay.Hide();
            }
        }

        private void Thoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        string oldConn =
"Server=.;Database=DennCoffee;Trusted_Connection=True;TrustServerCertificate=True";
        string newConn =
"Server=.;Database=TraSuaAppDb;Trusted_Connection=True;TrustServerCertificate=True";

        private async void click1(object sender, RoutedEventArgs e)
        {
            var importer = new KhachHangImporter(oldConn, newConn);
            await importer.ImportAsync();

            var importer2 = new HoaDonImporter(oldConn, newConn);
            await importer2.ImportTodayAsync();
        }



    }
}

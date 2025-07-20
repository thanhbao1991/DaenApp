using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

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
                    Content = (view.Name),
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
        private IKhachHangApi _api;
        private async void click1(object sender, RoutedEventArgs e)
        {
            _api = new KhachHangApi();
            var oldConn =
"Server=.;Database=DennCoffee;Trusted_Connection=True;TrustServerCertificate=True";

            var importer = new KhachHangImporter(oldConn, _api);
            await importer.ImportAsync();
        }


    }
}
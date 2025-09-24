using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class ThongKeTab : UserControl
    {
        private readonly ThongKeApi _api = new ThongKeApi();
        private DateTime _currentDate = DateTime.Today;

        public ThongKeTab()
        {
            InitializeComponent();
            _ = LoadAsync(_currentDate);
        }

        private async void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddDays(-1);
            await LoadAsync(_currentDate);
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddDays(1);
            await LoadAsync(_currentDate);
        }

        public async void ReloadToday()
        {
            _currentDate = DateTime.Today;
            await LoadAsync(_currentDate);
        }

        private async System.Threading.Tasks.Task LoadAsync(DateTime date)
        {
            DateTextBlock.Text = date.ToString("dd/MM/yyyy");

            var result = await _api.GetByDateAsync(date);
            if (result?.Data == null) return;

            var dto = result.Data;

            // ✅ Bind toàn bộ DTO cho XAML (bên trái cố định)
            this.DataContext = dto;

            // ===== Top sản phẩm bán chạy (Card thay DataGrid) =====
            TopSanPhamStackPanel.Children.Clear();
            TopSanPhamStackPanel.Children.Add(new TextBlock
            {
                Text = "Top sản phẩm bán chạy",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            });

            foreach (var sp in dto.TopSanPhams
                .OrderByDescending(x => x.DoanhThu)
                .ThenByDescending(x => x.SoLuong))
            {
                var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

                grid.Children.Add(new TextBlock
                {
                    Text = sp.TenSanPham,
                    FontSize = 14,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 8, 0)
                });

                var slText = new TextBlock
                {
                    Text = $"{sp.SoLuong:N0}",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(slText, 1);
                grid.Children.Add(slText);

                TopSanPhamStackPanel.Children.Add(grid);
            }

            // ===== Các card chi tiết bên phải (vẫn dynamic) =====
            FillStackPanel(DoanhThuStackPanel, "Doanh thu", dto.DoanhThu,
                dto.DoanhThuChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());

            FillStackPanel(ChiTieuStackPanel, "Chi tiêu", dto.ChiTieu,
                dto.ChiTieuChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());

            FillStackPanel(CongNoStackPanel, "Công nợ", dto.CongNo,
                dto.CongNoChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());

            FillStackPanel(TraNoTienStackPanel, "Trả nợ tiền", dto.TraNoTien,
                dto.TraNoTienChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());

            FillStackPanel(TraNoBankStackPanel, "Trả nợ bank", dto.TraNoBank,
                dto.TraNoBankChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());

            FillStackPanel(DaThuStackPanel, "Đã thu", dto.DaThu,
                dto.DaThuChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());

            FillStackPanel(ChuaThuStackPanel, "Chưa thu", dto.ChuaThu,
                dto.ChuaThuChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());
        }

        private void FillStackPanel(StackPanel panel, string title, decimal tong, params (string Ten, decimal GiaTri)[] details)
        {
            panel.Children.Clear();
            panel.Children.Add(new TextBlock
            {
                Text = $"{title}\n{tong:N0}",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            if (details == null) return;

            foreach (var (ten, giaTri) in details)
            {
                var grid = new Grid { Margin = new Thickness(0, 2, 0, 2), SnapsToDevicePixels = true };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

                grid.Children.Add(new TextBlock
                {
                    Text = ten,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 8, 0)
                });

                var valueText = new TextBlock
                {
                    Text = $"{giaTri:N0}",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 4, 0),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(valueText, 1);
                grid.Children.Add(valueText);

                panel.Children.Add(grid);
            }
        }
    }
}
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
        private const double ValueColWidth = 120;

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

        private async System.Threading.Tasks.Task LoadAsync(DateTime date)
        {
            DateTextBlock.Text = date.ToString("dd/MM/yyyy");

            var result = await _api.GetByDateAsync(date);
            if (result?.Data == null) return;
            var dto = result.Data;

            // ===== Top SP bán chạy (Card thay DataGrid) =====
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
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                // Tên sản phẩm
                grid.Children.Add(new TextBlock
                {
                    Text = sp.TenSanPham,
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.NoWrap,
                    Margin = new Thickness(0, 0, 8, 0)
                });

                // Số lượng
                var slText = new TextBlock
                {
                    Text = $"{sp.SoLuong:N0}",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(slText, 1);
                grid.Children.Add(slText);

                // Doanh thu
                var dtText = new TextBlock
                {
                    Text = $"{sp.DoanhThu:N0} đ",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(dtText, 2);
                grid.Children.Add(dtText);

                TopSanPhamStackPanel.Children.Add(grid);
            }

            // ===== Card kết quả bên trái (giữ nguyên) =====
            var items = new List<(string Ten, decimal GiaTri, bool IsChild)>
            {
                ("Doanh thu",      dto.DoanhThu,        false),
                ("Đã thu",         dto.DaThu,           false),
                ("Tiền mặt",       dto.DaThu_TienMat,   true),
                ("Banking",        dto.DaThu_Banking,   true),
                ("Khánh",          dto.DaThu_Khanh,     true),

                ("Chưa thu",       dto.ChuaThu,         false),
                ("Chi tiêu",       dto.ChiTieu,         false),
                ("Công nợ",        dto.CongNo,          false),
                ("Mang về",        dto.MangVe,          false),

                ("Trả nợ tiền",    dto.TraNoTien,       false),
                ("Trả nợ Khánh",   dto.TraNoKhanh,      true),
                ("Trả nợ Bank",    dto.TraNoBank,       true),

                ("Tổng số đơn",    dto.TongSoDon,       false),
                ("Tổng số ly",     dto.TongSoLy,        false)
            };

            KetQuaStackPanel.Children.Clear();
            foreach (var (ten, giatri, isChild) in items)
            {
                KetQuaStackPanel.Children.Add(BuildResultRow(ten, giatri, isChild));
                if (ten == "Mang về" || ten == "Trả nợ Bank")
                    KetQuaStackPanel.Children.Add(new Border { Height = 16, Background = Brushes.Transparent });
            }

            // ===== Các card chi tiết bên phải =====
            FillStackPanel(DoanhThuStackPanel, "Doanh thu", dto.DoanhThu,
                dto.DoanhThuChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());
            FillStackPanel(ChiTieuStackPanel, "Chi tiêu", dto.ChiTieu,
                dto.ChiTieuChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());
            FillStackPanel(CongNoStackPanel, "Ghi nợ", dto.CongNo,
                dto.CongNoChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());
            FillStackPanel(TraNoTienStackPanel, "Trả nợ tiền", dto.TraNoTien,
                dto.TraNoTienChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());
            FillStackPanel(TraNoBankStackPanel, "Trả nợ bank", dto.TraNoBank,
                dto.TraNoBankChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());
            FillStackPanel(DaThuStackPanel, "Đã thu", dto.DaThu,
                dto.DaThuChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());
            // ThongKeTab.xaml.cs (trong LoadAsync)
            FillStackPanel(ChuaThuStackPanel, "Chưa thu", dto.ChuaThu,
                dto.ChuaThuChiTiet.Select(x => (x.Ten, x.GiaTri)).ToArray());
        }

        private FrameworkElement BuildResultRow(string ten, decimal giatri, bool isChild)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4), SnapsToDevicePixels = true };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ValueColWidth) });

            grid.Children.Add(new TextBlock
            {
                Text = ten,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
                Margin = isChild ? new Thickness(20, 0, 8, 0) : new Thickness(0, 0, 8, 0)
            });

            var valueText = new TextBlock
            {
                Text = $"{giatri:N0}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Blue,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap
            };
            Grid.SetColumn(valueText, 1);
            grid.Children.Add(valueText);

            return grid;
        }

        private void FillStackPanel(StackPanel panel, string title, decimal tong, params (string Ten, decimal GiaTri)[] details)
        {
            panel.Children.Clear();
            panel.Children.Add(new TextBlock
            {
                Text = $"{title}\n{tong:N0} đ",
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
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                grid.Children.Add(new TextBlock
                {
                    Text = ten,
                    FontSize = 14,
                    TextWrapping = TextWrapping.NoWrap,
                    Margin = new Thickness(0, 0, 8, 0)
                });

                var valueText = new TextBlock
                {
                    Text = $"{giaTri:N0} đ",
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    TextWrapping = TextWrapping.NoWrap
                };
                Grid.SetColumn(valueText, 1);
                grid.Children.Add(valueText);

                panel.Children.Add(grid);
            }
        }
    }
}
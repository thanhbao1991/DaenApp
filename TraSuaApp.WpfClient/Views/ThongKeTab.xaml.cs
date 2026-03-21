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

        public decimal TienMatNha { get; private set; }
        public decimal ChiTieuNgay { get; private set; }

        public ThongKeTab()
        {
            InitializeComponent();
            _ = LoadAsync(_currentDate);
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddDays(-1);
            _ = LoadAsync(_currentDate);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddDays(1);
            _ = LoadAsync(_currentDate);
        }

        public void ReloadToday()
        {
            _currentDate = DateTime.Today;
            _ = LoadAsync(_currentDate);
        }

        private async Task LoadAsync(DateTime date)
        {
            try
            {
                DateTextBlock.Text = date.ToString("dd/MM/yyyy");

                TongChiTieuTextBlock.Text = "?";
                TongCongNoTextBlock.Text = "?";
                TongThanhToanTextBlock.Text = "?";
                TongDoanhThuTextBlock.Text = "?";
                TongTraNoTextBlock.Text = "?";

                ChiTieuNgayStackPanel.Children.Clear();
                ChiTieuThangStackPanel.Children.Clear();
                CongNoNgayStackPanel.Children.Clear();
                TienMatStackPanel.Children.Clear();
                ChuyenKhoanStackPanel.Children.Clear();
                DoanhThuNgayStackPanel.Children.Clear();
                TraNoTaiQuanStackPanel.Children.Clear();
                TraNoShipperStackPanel.Children.Clear();

                await Task.WhenAll(
                    LoadChiTieuAsync(date),
                    LoadCongNoAsync(date),
                    LoadThanhToanAsync(date),
                    LoadDoanhThuAsync(date),
                    LoadTraNoAsync(date)
                );

                ChuyenKhoanStackPanel.Children.Add(new Separator
                {
                    Margin = new Thickness(0, 4, 0, 4)
                });

                AddRow(
                    ChuyenKhoanStackPanel,
                    "KIỂM TIỀN",
                    TienMatNha - ChiTieuNgay,
                    ColorFromHex("#0b61d6")
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async Task LoadChiTieuAsync(DateTime date)
        {
            var result = await _api.GetByDateAsync(date);
            if (result?.Data == null) return;

            var dto = result.Data;

            TongChiTieuTextBlock.Text = dto.TongChiTieu.ToString("N0");

            AddRow(ChiTieuNgayStackPanel, "CHI TIÊU NGÀY", dto.ChiTieuNgay, ColorFromHex("#DC2626"));

            foreach (var it in dto.DanhSachChiTieuNgay)
                AddRow(ChiTieuNgayStackPanel, it.Ten, it.SoTien, ColorFromHex("#B91C1C"));

            AddRow(ChiTieuThangStackPanel, "CHI TIÊU THÁNG", dto.ChiTieuThang, ColorFromHex("#DC2626"));

            foreach (var it in dto.DanhSachChiTieuThang)
                AddRow(ChiTieuThangStackPanel, it.Ten, it.SoTien, ColorFromHex("#B91C1C"));

            ChiTieuNgay = dto.ChiTieuNgay;
        }

        private async Task LoadCongNoAsync(DateTime date)
        {
            var result = await _api.GetCongNoByDateAsync(date);
            if (result?.Data == null) return;

            var dto = result.Data;

            TongCongNoTextBlock.Text = dto.TongCongNoNgay.ToString("N0");

            foreach (var x in dto.DanhSachCongNoNgay)
                AddRow(CongNoNgayStackPanel, x.TenKhachHang, x.SoTienNo, ColorFromHex("#974A05"));
        }

        private async Task LoadThanhToanAsync(DateTime date)
        {
            var result = await _api.GetThanhToanByDateAsync(date);
            if (result?.Data == null) return;

            var dto = result.Data;

            TongThanhToanTextBlock.Text = (dto.TongTienMat + dto.TongChuyenKhoan).ToString("N0");

            foreach (var it in dto.DanhSachTienMat)
                AddRow(TienMatStackPanel, it.Ten, it.SoTien, ColorFromHex("#0b61d6"));

            if (dto.TongChuyenKhoan > 0)
                AddRow(ChuyenKhoanStackPanel, "Chuyển khoản", dto.TongChuyenKhoan, ColorFromHex("#0b61d6"));

            TienMatNha = dto.DanhSachTienMat[0].SoTien;
        }

        private async Task LoadDoanhThuAsync(DateTime date)
        {
            var result = await _api.GetDoanhThuNgayAsync(date);
            if (result?.Data == null) return;

            var dto = result.Data;

            TongDoanhThuTextBlock.Text = dto.TongDoanhThu.ToString("N0");

            foreach (var it in dto.DanhSach)
                AddRow(DoanhThuNgayStackPanel, it.Ten, it.DoanhThu, ColorFromHex("#0f5132"));
        }

        private async Task LoadTraNoAsync(DateTime date)
        {
            var result = await _api.GetTraNoNgayAsync(date);
            if (result?.Data == null) return;

            var dto = result.Data;

            TongTraNoTextBlock.Text =
                (dto.TongTraNoTaiQuan + dto.TongTraNoShipper).ToString("N0");

            AddRow(
                TraNoTaiQuanStackPanel,
                "TRẢ NỢ TẠI QUÁN",
                dto.TongTraNoTaiQuan,
                ColorFromHex("#6D28D9"));

            foreach (var x in dto.TraNoTaiQuan)
                AddRow(
                    TraNoTaiQuanStackPanel,
                    x.TenKhachHang,
                    x.SoTien,
                    ColorFromHex("#7C3AED"));

            AddRow(
                TraNoShipperStackPanel,
                "TRẢ NỢ SHIPPER",
                dto.TongTraNoShipper,
                ColorFromHex("#6D28D9"));

            foreach (var x in dto.TraNoShipper)
                AddRow(
                    TraNoShipperStackPanel,
                    x.TenKhachHang,
                    x.SoTien,
                    ColorFromHex("#7C3AED"));
        }

        private void AddRow(StackPanel panel, string name, decimal value, Color amountColor)
        {
            var rowBorder = new Border();

            if (TryFindResource("RowBorderStyle") is Style rowStyle)
                rowBorder.Style = rowStyle;

            var grid = new Grid();

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameTb = new TextBlock
            {
                Text = name,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(amountColor)
            };

            var valTb = new TextBlock
            {
                Text = value.ToString("N0"),
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(ColorFromHex("#334155"))

            };

            Grid.SetColumn(valTb, 1);

            grid.Children.Add(nameTb);
            grid.Children.Add(valTb);

            rowBorder.Child = grid;

            panel.Children.Add(rowBorder);
        }

        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
    }
}
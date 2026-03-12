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

        // navigation
        private async void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddDays(-1);
            _ = LoadAsync(_currentDate);
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddDays(1);
            _ = LoadAsync(_currentDate);
        }

        public async void ReloadToday()
        {
            _currentDate = DateTime.Today;
            _ = LoadAsync(_currentDate);
        }

        // main loader (keeps your original signature)
        private async Task LoadAsync(DateTime date)
        {
            try
            {
                DateTextBlock.Text = date.ToString("dd/MM/yyyy");

                // placeholders while loading
                TongChiTieuTextBlock.Text = "?";
                TongCongNoTextBlock.Text = "?";
                TongThanhToanTextBlock.Text = "?";
                TongDoanhThuTextBlock.Text = "?";

                // clear panels
                ChiTieuNgayStackPanel.Children.Clear();
                ChiTieuThangStackPanel.Children.Clear();
                CongNoNgayStackPanel.Children.Clear();
                TienMatStackPanel.Children.Clear();
                ChuyenKhoanStackPanel.Children.Clear();
                DoanhThuNgayStackPanel.Children.Clear();

                await Task.WhenAll(
                    LoadChiTieuAsync(date),
                    LoadCongNoAsync(date),
                    LoadThanhToanAsync(date),
                    LoadDoanhThuAsync(date)
                );
                // separator
                ChuyenKhoanStackPanel.Children.Add(new Separator
                {
                    Margin = new Thickness(0, 4, 0, 4)
                });
                AddRow(ChuyenKhoanStackPanel, "KIỂM TIỀN", TienMatNha - ChiTieuNgay, ColorFromHex("#0b61d6"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // ------------------- loaders -------------------

        private async Task LoadChiTieuAsync(DateTime date)
        {
            var result = await _api.GetByDateAsync(date);

            if (result?.Data == null)
                return;

            var dto = result.Data;

            // Tổng chi tiêu (header card)
            TongChiTieuTextBlock.Text = dto.TongChiTieu.ToString("N0");

            // ----- Chi tiêu ngày -----
            ChiTieuNgayStackPanel.Children.Clear();

            // Tổng ngày
            AddRow(
                ChiTieuNgayStackPanel,
                "CHI TIÊU NGÀY",
                dto.ChiTieuNgay,
                ColorFromHex("#DC2626"));

            // danh sách chi tiết
            foreach (var it in dto.DanhSachChiTieuNgay)
                AddRow(
                    ChiTieuNgayStackPanel,
                    it.Ten,
                    it.SoTien,
                    ColorFromHex("#B91C1C"));



            // ----- Chi tiêu tháng -----
            ChiTieuThangStackPanel.Children.Clear();

            // Tổng tháng
            AddRow(
                ChiTieuThangStackPanel,
                "CHI TIÊU THÁNG",
                dto.ChiTieuThang,
                ColorFromHex("#DC2626"));

            foreach (var it in dto.DanhSachChiTieuThang)
                AddRow(
                    ChiTieuThangStackPanel,
                    it.Ten,
                    it.SoTien,
                    ColorFromHex("#B91C1C"));

            ChiTieuNgay = dto.ChiTieuNgay;
        }

        private async Task LoadCongNoAsync(DateTime date)
        {
            var result = await _api.GetCongNoByDateAsync(date);

            if (result?.Data == null)
                return;

            var dto = result.Data;

            // Tổng công nợ
            TongCongNoTextBlock.Text = dto.TongCongNoNgay.ToString("N0");

            // Danh sách công nợ trong ngày
            CongNoNgayStackPanel.Children.Clear();
            foreach (var x in dto.DanhSachCongNoNgay)
                AddRow(CongNoNgayStackPanel, x.TenKhachHang, x.SoTienNo, ColorFromHex("#974A05"));
        }

        private async Task LoadThanhToanAsync(DateTime date)
        {
            var result = await _api.GetThanhToanByDateAsync(date);

            if (result?.Data == null)
                return;

            var dto = result.Data;

            TongThanhToanTextBlock.Text = (dto.TongTienMat + dto.TongChuyenKhoan).ToString("N0");

            // Tiền mặt (danh sách chi tiết)
            TienMatStackPanel.Children.Clear();
            foreach (var it in dto.DanhSachTienMat)
                AddRow(TienMatStackPanel, it.Ten, it.SoTien, ColorFromHex("#0b61d6"));

            // Chuyển khoản (tổng)
            ChuyenKhoanStackPanel.Children.Clear();
            if (dto.TongChuyenKhoan > 0)
                AddRow(ChuyenKhoanStackPanel, "Chuyển khoản", dto.TongChuyenKhoan, ColorFromHex("#0b61d6"));

            TienMatNha = dto.DanhSachTienMat[0].SoTien;
        }

        private async Task LoadDoanhThuAsync(DateTime date)
        {
            var result = await _api.GetDoanhThuNgayAsync(date);

            if (result?.Data == null)
                return;

            var dto = result.Data;

            TongDoanhThuTextBlock.Text = dto.TongDoanhThu.ToString("N0");

            DoanhThuNgayStackPanel.Children.Clear();
            foreach (var it in dto.DanhSach)
                AddRow(DoanhThuNgayStackPanel, it.Ten, it.DoanhThu, ColorFromHex("#0f5132"));
        }

        // ------------------- helpers -------------------
        private void AddRow(StackPanel panel, string name, decimal value, Color amountColor)
        {
            var rowBorder = new Border();
            try
            {
                // Apply row style from XAML if available (hover effect)
                if (TryFindResource("RowBorderStyle") is Style rowStyle)
                    rowBorder.Style = rowStyle;
                else
                {
                    rowBorder.Padding = new Thickness(6);
                    rowBorder.Margin = new Thickness(0, 4, 0, 4);
                }
            }
            catch
            {
                rowBorder.Padding = new Thickness(6);
                rowBorder.Margin = new Thickness(0, 4, 0, 4);
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameTb = new TextBlock
            {
                Text = name,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };

            var valTb = new TextBlock
            {
                Text = value.ToString("N0"),
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(amountColor),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
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
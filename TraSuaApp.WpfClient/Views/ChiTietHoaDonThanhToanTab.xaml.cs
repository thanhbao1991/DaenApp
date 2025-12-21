using System.Diagnostics;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;

namespace TraSuaApp.WpfClient.Views
{
    public partial class ChiTietHoaDonThanhToanTab : UserControl
    {

        private readonly DebounceManager _debouncer = new();
        private readonly WpfErrorHandler _errorHandler = new();

        private List<ChiTietHoaDonThanhToanDto> _fullChiTietHoaDonThanhToanList = new();

        // Cho phép Dashboard đẩy "today" đang dùng (nếu có)
        public DateTime Today { get; set; } = DateTime.Today;

        // Expose SelectedItem nếu cần hotkey sau này
        public ChiTietHoaDonThanhToanDto? SelectedThanhToan
            => ChiTietHoaDonThanhToanDataGrid.SelectedItem as ChiTietHoaDonThanhToanDto;

        public ChiTietHoaDonThanhToanTab()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                // tránh null khi mở form
                var sw = Stopwatch.StartNew();
                while (AppProviders.ChiTietHoaDonThanhToans?.Items == null && sw.ElapsedMilliseconds < 5000)
                    await Task.Delay(100);

                await ReloadUI();
            };
        }

        public async Task ReloadUI()
        {
            var todayLocal = Today;
            //var todayLocal = Today.AddDays(-1);

            _fullChiTietHoaDonThanhToanList = await UiListHelper.BuildListAsync(
                AppProviders.ChiTietHoaDonThanhToans.Items.Where(x => !x.IsDeleted),
                snap => snap.Where(x => x.Ngay == todayLocal)
                            .OrderByDescending(x => x.NgayGio)
                            .ToList()
            );

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string keyword = (SearchChiTietHoaDonThanhToanTextBox.Text ?? string.Empty).Trim().ToLower();
            decimal tongTien = 0;
            List<ChiTietHoaDonThanhToanDto> sourceList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                sourceList = _fullChiTietHoaDonThanhToanList;
            }
            else
            {
                // Giống code cũ: chuẩn hoá text rồi phải khớp TẤT CẢ từ khoá
                keyword = StringHelper.MyNormalizeText(keyword);
                var keywords = keyword
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(k => k.ToLower())
                    .ToList();

                sourceList = _fullChiTietHoaDonThanhToanList
                    .Where(x =>
                    {
                        var text = x.TimKiem.ToLower();
                        return keywords.All(k => text.Contains(k));
                    })
                    .ToList();
            }

            int stt = 1;
            foreach (var item in sourceList) item.Stt = stt++;

            ChiTietHoaDonThanhToanDataGrid.ItemsSource = sourceList;
            tongTien = sourceList.Sum(x => x.SoTien);

            TongTienThanhToanTextBlock.Header = $"{tongTien:N0} đ";
        }

        private void SearchChiTietHoaDonThanhToanTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debouncer.Debounce("ChiTietHoaDonThanhToan", 300, ApplyFilter);
        }

        private async void ChiTietHoaDonThanhToanDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChiTietHoaDonThanhToanDataGrid.SelectedItem is not ChiTietHoaDonThanhToanDto selected) return;

            var owner = Window.GetWindow(this);
            var window = new ChiTietHoaDonThanhToanEdit(selected)
            {
                Width = owner?.ActualWidth ?? 1200,
                Height = owner?.ActualHeight ?? 800,
                Owner = owner
            };

            if (window.ShowDialog() == true)
            {
                await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
            }
        }

        private async void XoaChiTietHoaDonThanhToanButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTietHoaDonThanhToanDataGrid.SelectedItem is not ChiTietHoaDonThanhToanDto selected)
                return;

            var confirm = MessageBox.Show(
              $"Bạn có chắc chắn muốn xoá '{selected.Ten}'?",
              "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/ChiTietHoaDonThanhToan/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<ChiTietHoaDonThanhToanDto>>();

                if (result?.IsSuccess == true)
                {
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
                }
                else
                {
                    _errorHandler.Handle(new Exception(result?.Message ?? "Không thể xoá."), "Delete");
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Delete");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task ReloadAfterHoaDonChangeAsync(
            bool reloadHoaDon = true,
            bool reloadThanhToan = false,
            bool reloadNo = false,
            bool reloadChiTieu = false)
        {
            if (reloadHoaDon)
                await AppProviders.HoaDons.ReloadAsync();

            if (reloadThanhToan)
                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();

            if (reloadNo)
                await AppProviders.ChiTietHoaDonNos.ReloadAsync();

            if (reloadChiTieu)
                await AppProviders.ChiTieuHangNgays.ReloadAsync();

            // refresh lại UI của tab sau khi dữ liệu cập nhật
            await ReloadUI();
        }

        private void TimKiemNhanhThanhToanButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
                SearchChiTietHoaDonThanhToanTextBox.Text = bt.Tag?.ToString() ?? "";
        }
    }
}
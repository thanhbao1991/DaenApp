using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Constants;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class ChiTietHoaDonNoTab : UserControl
    {
        private readonly DebounceManager _debouncer = new();
        private readonly DashboardApi _api = new();

        private List<HoaDonNoDto> _items = new();

        public ChiTietHoaDonNoTab()
        {
            InitializeComponent();

            Loaded += async (_, __) =>
            {
                await ReloadUI();
            };
        }

        public HoaDonNoDto? SelectedNo =>
            ChiTietHoaDonNoDataGrid.SelectedItem as HoaDonNoDto;

        // ================================
        // LOAD DATA
        // ================================

        public async Task ReloadUI()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var response = await _api.GetCongNo();

                _items = response.IsSuccess
                    ? response.Data ?? new()
                    : new();

                ApplyFilter();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        // ================================
        // SEARCH
        // ================================

        private void SearchChiTietHoaDonNoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debouncer.Debounce("CongNoSearch", 300, ApplyFilter);
        }

        private void ApplyFilter()
        {
            string keyword = StringHelper.MyNormalizeText(
                SearchChiTietHoaDonNoTextBox.Text ?? ""
            );

            List<HoaDonNoDto> list = string.IsNullOrWhiteSpace(keyword)
                ? _items
                : _items
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();

            int stt = 1;
            foreach (var item in list)
                item.Stt = stt++;

            ChiTietHoaDonNoDataGrid.ItemsSource = list;

            TongTienChiTietHoaDonNoTextBlock.Header =
                $"{list.Sum(x => x.ConLai):N0}";
        }

        // ================================
        // QUICK FILTER
        // ================================

        private void TimKiemNhanhCongNoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
            {
                var tag = bt.Tag?.ToString();

                if (tag == "hôm nay")
                    SearchChiTietHoaDonNoTextBox.Text =
                        DateTime.Today.ToString("dd-MM-yyyy");

                else if (tag == "hôm qua")
                    SearchChiTietHoaDonNoTextBox.Text =
                        DateTime.Today.AddDays(-1).ToString("dd-MM-yyyy");
            }
        }

        // ================================
        // PAY FROM ROW
        // ================================

        private async void TienMatRowButton_Click(object sender, RoutedEventArgs e)
        {
            await PayFromRow(sender, AppConstants.TienMatId);
        }

        private async void ChuyenKhoanRowButton_Click(object sender, RoutedEventArgs e)
        {
            await PayFromRow(sender, AppConstants.ChuyenKhoanId);
        }

        private async Task PayFromRow(object sender, Guid methodId)
        {
            if (sender is Button bt && bt.DataContext is HoaDonNoDto item)
            {
                bool quickPay = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

                await PayItemAsync(item, methodId, quickPay, bt);
            }
        }

        // ================================
        // PAY LOGIC
        // ================================

        private async Task PayItemAsync(
       HoaDonNoDto item,
       Guid methodId,
       bool quickPay,
       Button? btn = null)
        {
            if (item.ConLai == 0)
            {
                NotiHelper.Show("Công nợ đã thu đủ!");
                return;
            }

            IDisposable? busy = null;

            try
            {
                var owner = Window.GetWindow(this);
                var now = DateTime.Now;

                busy = BusyUI.Scope(this, btn, "Đang mở form...");

                var dto = new ChiTietHoaDonThanhToanDto
                {
                    Ngay = now.Date,
                    NgayGio = now,
                    HoaDonId = item.Id,
                    KhachHangId = item.KhachHangId,
                    Ten = item.TenKhachHangText,
                    PhuongThucThanhToanId = methodId,
                    LoaiThanhToan =
                        item.NgayNo?.Date == now.Date
                        ? "Trả nợ trong ngày"
                        : "Trả nợ qua ngày",
                    GhiChu = item.GhiChu,
                    SoTien = item.ConLai
                };

                busy.Dispose();
                busy = null;

                var win = new ChiTietHoaDonThanhToanEdit(dto)
                {
                    Owner = owner,
                    Width = owner?.ActualWidth ?? 1200,
                    Height = owner?.ActualHeight ?? 800
                };

                win.PhuongThucThanhToanComboBox.IsEnabled = false;

                // ===============================
                // CTRL = THU NHANH
                // ===============================

                if (quickPay)
                {
                    win.QuickSubmit = true;
                    win.WindowStartupLocation = WindowStartupLocation.Manual;
                    win.Left = -10000;
                    win.Top = -10000;
                    win.ShowInTaskbar = false;
                }

                if (win.ShowDialog() == true)
                {
                    await ReloadAfterPay();
                }
            }
            catch (Exception ex)
            {
                NotiHelper.Show($"Lỗi: {ex.Message}");
            }
            finally
            {
                busy?.Dispose();
            }
        }
        // ================================
        // RELOAD AFTER PAY
        // ================================

        private async Task ReloadAfterPay()
        {
            await AppProviders.HoaDons.ReloadAsync();
            await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();

            await ReloadUI();
        }

        // ================================
        // HOTKEYS
        // ================================

        public async void HandleHotkey(Key key)
        {
            var item = SelectedNo;

            if (item == null)
            {
                NotiHelper.Show("Vui lòng chọn công nợ!");
                return;
            }

            switch (key)
            {
                case Key.F1:
                    await PayItemAsync(item, AppConstants.TienMatId, false);
                    break;

                case Key.F4:
                    await PayItemAsync(item, AppConstants.ChuyenKhoanId, false);
                    break;
            }
        }
    }
}
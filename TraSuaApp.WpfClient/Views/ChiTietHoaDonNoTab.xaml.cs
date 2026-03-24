using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Constants;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class ChiTietHoaDonNoTab : UserControl
    {
        private readonly DebounceManager _debouncer = new();
        private readonly DashboardApi _api = new();
        private readonly HoaDonApi _hoaDonApi = new();

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
                    .Where(x => x.TimKiem.ToLower().Contains(keyword.ToLower()))
                    .ToList();

            int stt = 1;
            foreach (var item in list)
                item.Stt = stt++;

            ChiTietHoaDonNoDataGrid.ItemsSource = list;

            TongTienChiTietHoaDonNoTextBlock.Header =
                $"{list.Sum(x => x.ConLai):N0}";
        }

        // ================================
        // ROW BUTTONS
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

                bt.IsEnabled = false;

                await ThanhToanNoAsync(item, methodId, quickPay);

                bt.IsEnabled = true;
            }
        }

        // ================================
        // CORE PAYMENT LOGIC
        // ================================
        private async Task ThanhToanNoAsync(
            HoaDonNoDto item,
            Guid phuongThucId,
            bool quickPay = false)
        {
            if (item == null)
                return;

            if (item.ConLai <= 0)
            {
                NotiHelper.Show("Công nợ đã thu đủ!");
                return;
            }

            var now = DateTime.Now;

            DateTime ngay;
            DateTime ngayGio;

            if (item.NgayGio.HasValue && now.Date > item.NgayGio.Value.Date)
            {
                ngayGio = item.NgayGio.Value.Date.AddDays(1).AddSeconds(-1);
                ngay = ngayGio.Date;
            }
            else
            {
                ngayGio = now;
                ngay = now.Date;
            }

            decimal soTien;

            // ==============================
            // QUICK PAY (CTRL)
            // ==============================
            if (quickPay)
            {
                soTien = item.ConLai;
            }
            else
            {
                var dto = new ChiTietHoaDonThanhToanDto
                {
                    HoaDonId = item.Id,
                    KhachHangId = item.KhachHangId,
                    Ten = item.TenKhachHangText,
                    SoTien = item.ConLai,
                    PhuongThucThanhToanId = phuongThucId,
                };
                var owner = Window.GetWindow(this);

                var form = new ChiTietHoaDonThanhToanEdit(dto)
                {
                    Owner = owner,
                    Width = owner?.ActualWidth ?? 1200,
                    Height = owner?.ActualHeight ?? 800
                };

                form.PhuongThucThanhToanComboBox.IsEnabled = false;

                if (form.ShowDialog() != true)
                    return;

                soTien = form.Model.SoTien;

                if (soTien <= 0)
                    return;

                if (soTien > item.ConLai)
                    soTien = item.ConLai;
            }

            // ==============================
            // BACKUP
            // ==============================
            var oldConLai = item.ConLai;
            var oldDaThu = item.DaThu;
            var oldLastModified = item.LastModified;

            // ==============================
            // OPTIMISTIC UI
            // ==============================
            item.DaThu += soTien;
            item.ConLai = item.ThanhTien - item.DaThu;

            if (item.ConLai <= 0)
            {
                _items = _items.Where(x => x.Id != item.Id).ToList();
                ChiTietHoaDonNoDataGrid.SelectedItem = null;
            }

            ApplyFilter();

            var lastModified = item.LastModified;

            // ==============================
            // CALL API
            // ==============================
            _ = Task.Run(async () =>
            {
                try
                {
                    var newDto = new ChiTietHoaDonThanhToanDto
                    {
                        LastModified = lastModified,
                        HoaDonId = item.Id,
                        KhachHangId = item.KhachHangId,
                        Ten = item.TenKhachHangText,
                        SoTien = soTien,
                        PhuongThucThanhToanId = phuongThucId,
                    };

                    var r = await _hoaDonApi.UpdateF1F4SingleAsync(item.Id, newDto);

                    if (!r.IsSuccess)
                        NotiHelper.ShowError(r.Message);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (r.Data != null)
                        {
                            item.DaThu = r.Data.DaThu;
                            item.ConLai = r.Data.ConLai;
                            item.LastModified = r.Data.LastModified;

                            if (item.ConLai <= 0)
                            {
                                _items = _items.Where(x => x.Id != item.Id).ToList();
                                ChiTietHoaDonNoDataGrid.SelectedItem = null;
                            }

                            ApplyFilter();
                        }
                    });
                }
                catch
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        item.ConLai = oldConLai;
                        item.DaThu = oldDaThu;
                        item.LastModified = oldLastModified;

                        if (!_items.Any(x => x.Id == item.Id))
                        {
                            _items.Add(item);
                        }

                        ApplyFilter();

                        NotiHelper.ShowError("Thanh toán thất bại!");
                    });
                }
            });
        }

        // ================================
        // HOTKEY
        // ================================
        public async void HandleHotkey(Key key)
        {
            var item = SelectedNo;

            if (item == null)
            {
                //NotiHelper.Show("Vui lòng chọn công nợ!");
                return;
            }

            bool quickPay = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            switch (key)
            {
                case Key.F1:
                    await ThanhToanNoAsync(item, AppConstants.TienMatId, quickPay);
                    break;

                case Key.F4:
                    await ThanhToanNoAsync(item, AppConstants.ChuyenKhoanId, quickPay);
                    break;
            }
        }
    }
}
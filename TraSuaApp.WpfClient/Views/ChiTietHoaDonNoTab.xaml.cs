using System.Diagnostics;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private readonly WpfErrorHandler _errorHandler = new();

        private List<ChiTietHoaDonNoDto> _fullChiTietHoaDonNoList = new();
        private ChiTietHoaDonNoApi _Api;

        public ChiTietHoaDonNoTab()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                // đợi provider sẵn sàng (tránh null lúc mở window)
                var sw = Stopwatch.StartNew();
                while (AppProviders.ChiTietHoaDonNos?.Items == null && sw.ElapsedMilliseconds < 5000)
                    await Task.Delay(100);

                await ReloadUI();
            };
            _Api = new ChiTietHoaDonNoApi();

        }

        // Expose SelectedItem để Dashboard dùng phím tắt F1/F4/F5 như cũ
        public ChiTietHoaDonNoDto? SelectedNo
            => ChiTietHoaDonNoDataGrid.SelectedItem as ChiTietHoaDonNoDto;

        public async Task ReloadUI()
        {
            var todayLocal = DateTime.Today;

            _fullChiTietHoaDonNoList = await UiListHelper.BuildListAsync(
                AppProviders.ChiTietHoaDonNos.Items.Where(x => !x.IsDeleted),
                snap => snap.Where(x => x.SoTienConLai > 0 || x.Ngay == todayLocal)
                            .OrderByDescending(x => x.LastModified)
                            .ToList()
            );

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string keyword = StringHelper.MyNormalizeText(SearchChiTietHoaDonNoTextBox.Text ?? "");
            List<ChiTietHoaDonNoDto> sourceList = string.IsNullOrWhiteSpace(keyword)
                ? _fullChiTietHoaDonNoList
                : _fullChiTietHoaDonNoList.Where(x => x.TimKiem.ToLower().Contains(keyword)).ToList();

            int stt = 1; foreach (var item in sourceList) item.Stt = stt++;
            ChiTietHoaDonNoDataGrid.ItemsSource = sourceList;
            TongTienChiTietHoaDonNoTextBlock.Header = $"{sourceList.Sum(x => x.SoTienConLai):N0} đ";
        }

        private void SearchChiTietHoaDonNoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debouncer.Debounce("ChiTietHoaDonNo", 300, ApplyFilter);
        }

        private async void ChiTietHoaDonNoDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected) return;

            var now = DateTime.Now;
            var dto = new ChiTietHoaDonThanhToanDto
            {
                ChiTietHoaDonNoId = selected.Id,
                Ngay = now.Date,
                NgayGio = now,
                HoaDonId = selected.HoaDonId,
                KhachHangId = selected.KhachHangId,
                Ten = $"{selected.Ten}",
                LoaiThanhToan = selected.Ngay == now.Date ? "Trả nợ trong ngày" : "Trả nợ qua ngày",
                GhiChu = selected.GhiChu,
                SoTien = selected.SoTienConLai,
            };

            var owner = Window.GetWindow(this);
            var window = new ChiTietHoaDonThanhToanEdit(dto)
            {
                Width = owner?.ActualWidth ?? 1200,
                Height = owner?.ActualHeight ?? 800,
                Owner = owner
            };

            if (window.ShowDialog() == true)
            {
                await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadNo: true, reloadThanhToan: true);
            }
        }

        private async void XoaChiTietHoaDonNoButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected)
                return;

            var confirm = MessageBox.Show(
               $"Bạn có chắc chắn muốn xoá '{selected.Ten}'?",
               "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/ChiTietHoaDonNo/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<ChiTietHoaDonNoDto>>();

                if (result?.IsSuccess == true)
                {
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadNo: true);
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

            await ReloadUI();
        }

        private void TimKiemNhanhCongNoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
            {
                var tag = bt.Tag?.ToString();
                if (tag == "hôm nay")
                    SearchChiTietHoaDonNoTextBox.Text = DateTime.Today.ToString("dd-MM-yyyy");
                else if (tag == "hôm qua")
                    SearchChiTietHoaDonNoTextBox.Text = DateTime.Today.AddDays(-1).ToString("dd-MM-yyyy");
            }
        }
        private async void F1aButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected) { NotiHelper.Show("Vui lòng chọn công nợ!"); return; }
            if (selected.SoTienConLai == 0) { NotiHelper.Show("Công nợ đã thu đủ!"); return; }

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var result = await _Api.PayAsync(selected.Id, "TienMat");
                if (result.IsSuccess) await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadNo: true, reloadThanhToan: true);
                else NotiHelper.Show(result.Message ?? "Không thể thanh toán.");
            }
            finally { Mouse.OverrideCursor = null; }
        }

        private async void F4aButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected) { NotiHelper.Show("Vui lòng chọn công nợ!"); return; }
            if (selected.SoTienConLai == 0) { NotiHelper.Show("Công nợ đã thu đủ!"); return; }

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var result = await _Api.PayAsync(selected.Id, "ChuyenKhoan");
                if (result.IsSuccess) await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadNo: true, reloadThanhToan: true);
                else NotiHelper.Show(result.Message ?? "Không thể thanh toán.");
            }
            finally { Mouse.OverrideCursor = null; }
        }



        //private async void F5aButton_Click(object sender, RoutedEventArgs e)
        //{
        //    return; // ❗Bỏ dòng này nếu muốn bật F5a

        //    if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected)
        //    {
        //        NotiHelper.Show("Vui lòng chọn công nợ!");
        //        return;
        //    }
        //    if (selected.SoTienConLai == 0)
        //    {
        //        NotiHelper.Show("Công nợ đã thu đủ!");
        //        return;
        //    }

        //    var owner = Window.GetWindow(this);
        //    var dto = CreateDtoTraNo(selected, Guid.Parse("3d75dd9f-a5d3-491d-a316-6d5c9ff7e66c"));
        //    var window = new ChiTietHoaDonThanhToanEdit(dto)
        //    {
        //        Owner = owner,                                 // ✅
        //        Width = owner?.ActualWidth ?? ActualWidth,
        //        Height = owner?.ActualHeight ?? ActualHeight
        //    };
        //    window.PhuongThucThanhToanComboBox.IsEnabled = false;

        //    if (window.ShowDialog() == true)
        //        await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
        //}
        //private async void F4aButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected)
        //    {
        //        NotiHelper.Show("Vui lòng chọn công nợ!");
        //        return;
        //    }
        //    if (selected.SoTienConLai == 0)
        //    {
        //        NotiHelper.Show("Công nợ đã thu đủ!");
        //        return;
        //    }

        //    var owner = Window.GetWindow(this);
        //    var dto = CreateDtoTraNo(selected, Guid.Parse("2cf9a88f-3bc0-4d4b-940d-f8ffa4affa02"));
        //    var window = new ChiTietHoaDonThanhToanEdit(dto)
        //    {
        //        Owner = owner,                                 // ✅
        //        Width = owner?.ActualWidth ?? ActualWidth,
        //        Height = owner?.ActualHeight ?? ActualHeight
        //    };
        //    window.PhuongThucThanhToanComboBox.IsEnabled = false;

        //    if (window.ShowDialog() == true)
        //        await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
        //}
        //private async void F1aButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected)
        //    {
        //        NotiHelper.Show("Vui lòng chọn công nợ!");
        //        return;
        //    }
        //    if (selected.SoTienConLai == 0)
        //    {
        //        NotiHelper.Show("Công nợ đã thu đủ!");
        //        return;
        //    }

        //    var owner = Window.GetWindow(this);
        //    var dto = CreateDtoTraNo(selected, Guid.Parse("0121FC04-0469-4908-8B9A-7002F860FB5C"));
        //    var window = new ChiTietHoaDonThanhToanEdit(dto)
        //    {
        //        Owner = owner,                                 // ✅ Owner là Window
        //        Width = owner?.ActualWidth ?? ActualWidth,     // kích thước hợp lý
        //        Height = owner?.ActualHeight ?? ActualHeight
        //    };
        //    window.PhuongThucThanhToanComboBox.IsEnabled = false;

        //    if (window.ShowDialog() == true)
        //        await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
        //}

        private ChiTietHoaDonThanhToanDto CreateDtoTraNo(ChiTietHoaDonNoDto selected, Guid phuongThucId)
        {
            var now = DateTime.Now;
            return new ChiTietHoaDonThanhToanDto
            {
                ChiTietHoaDonNoId = selected.Id,
                Ngay = now.Date,
                NgayGio = now,
                HoaDonId = selected.HoaDonId,
                KhachHangId = selected.KhachHangId,
                Ten = $"{selected.Ten}",
                PhuongThucThanhToanId = phuongThucId,
                LoaiThanhToan = selected.Ngay == now.Date ? "Trả nợ trong ngày" : "Trả nợ qua ngày",
                GhiChu = selected.GhiChu,
                SoTien = selected.SoTienConLai,
            };
        }
        public void HandleHotkey(Key key)
        {
            switch (key)
            {
                case Key.F1: F1aButton_Click(this, new RoutedEventArgs()); break;
                case Key.F4: F4aButton_Click(this, new RoutedEventArgs()); break;
                    //    case Key.F5: F5aButton_Click(this, new RoutedEventArgs()); break;
            }
        }


    }
}
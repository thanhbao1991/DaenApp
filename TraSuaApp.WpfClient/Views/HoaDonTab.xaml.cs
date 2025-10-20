using System.Diagnostics;
using System.IO;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FontAwesome.Sharp;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class HoaDonTab : UserControl
    {
        // ==================== FIELDS ====================
        private readonly DebounceManager _debouncer = new();
        private CancellationTokenSource? _cts;
        private List<HoaDonDto> _fullHoaDonList = new();
        private List<ChiTietHoaDonDto> _fullChiTietHoaDonList = new();
        private readonly int _hoaDonDueBatchSize = 3;
        private readonly DateTime today = DateTime.Today;

        // ==================== CTOR ======================
        public HoaDonTab()
        {
            InitializeComponent();
            Loaded += HoaDonTab_Loaded;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object? s, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async void HoaDonTab_Loaded(object? sender, RoutedEventArgs e)
        {
            // Khởi tạo combos giờ/phút cho hẹn giờ
            if (GioCombo != null) GioCombo.ItemsSource = Enumerable.Range(0, 24).Select(i => i.ToString("00"));
            if (PhutCombo != null) PhutCombo.ItemsSource = Enumerable.Range(0, 60).Select(i => i.ToString("00"));

            await AppProviders.HoaDons.ReloadAsync();
            ReloadHoaDonUI();
        }

        // ==================== DEBOUNCE & UTIL ====================
        private void DebounceSearch(TextBox tb, string key, Action applyFilter, int delayMs = 300)
        {
            _debouncer.Debounce(key, delayMs, applyFilter);
        }

        private async Task<bool> WaitUntilAsync(Func<bool> predicate, int timeoutMs = 6000, int intervalMs = 50)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                try { if (predicate()) return true; } catch { }
                await Task.Delay(intervalMs);
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
            }
            return predicate();
        }

        // ==================== RELOAD & FILTER (LIST) ====================
        public async void ReloadHoaDonUI()
        {
            _fullHoaDonList = await UiListHelper.BuildListAsync(
                AppProviders.HoaDons.Items.Where(x => !x.IsDeleted),
                snap => snap.Where(x => x.Ngay.Date == today.Date || x.DaThuHoacGhiNo)
                    .OrderBy(x =>
                    {
                        // Ưu tiên nhóm (y như code gốc)
                        if (x.UuTien) return 0; // Nhóm 1: ưu tiên
                        if (x.PhanLoai == "Ship" && x.NgayShip == null) return 1; // Nhóm 2: ship chưa đi
                        if (x.PhanLoai != "Ship" &&
                            (x.TrangThai == "Chưa thu" || x.TrangThai == "Thu một phần" || x.TrangThai == "Chuyển khoản một phần"))
                            return 2; // Nhóm 3: chưa thu (không phải ship)
                        if (x.PhanLoai == "Ship" && x.NgayShip != null && !x.DaThuHoacGhiNo) return 3; // Nhóm 4: ship đã đi nhưng chưa thu
                        return 4; // Nhóm 5: còn lại
                    })
                    .ThenByDescending(x => x.NgayGio)
            );

            ApplyHoaDonFilter();
        }

        private void ApplyHoaDonFilter()
        {
            string keyword = (SearchHoaDonTextBox.Text ?? "").Trim().ToLower();
            List<HoaDonDto> sourceList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                sourceList = _fullHoaDonList;
            }
            else
            {
                // match đúng hành vi gốc: lọc theo TimKiem chứa keyword (lower)
                sourceList = _fullHoaDonList
                    .Where(x => (x.TimKiem ?? $"{x.Ten} {x.TrangThai} {x.PhanLoai} {x.DiaChiText}").ToLower().Contains(keyword))
                    .ToList();
            }

            // Gán STT như code gốc
            int stt = 1;
            foreach (var item in sourceList) item.Stt = stt++;

            HoaDonDataGrid.ItemsSource = sourceList;
        }

        // ==================== HANDLERS: TÌM KIẾM LIST ====================
        private void SearchHoaDonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                DebounceSearch(tb, "HoaDon", ApplyHoaDonFilter, 300);
        }

        private void SearchHoaDonTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // hành vi gốc: trả về height 32
            try { SearchHoaDonTextBox.Height = 32; } catch { }
        }

        // ==================== NEW HĐ THEO PHÂN LOẠI ====================
        private async void OpenHoaDonWithPhanLoai(string phanLoai)
        {
            var dto = new HoaDonDto { PhanLoai = phanLoai };
            var window = new HoaDonEdit(dto)
            {
                Owner = Window.GetWindow(this),
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (window.ShowDialog() == true)
            {
                var savedId = window.SavedHoaDonId ?? window.Model?.Id;

                await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);

                try { SearchHoaDonTextBox?.Clear(); } catch { }

                if (savedId.HasValue && savedId.Value != Guid.Empty)
                    await SelectHoaDonByIdAsync(savedId.Value);
                else
                    await SelectNewestHoaDonRowAsync();
            }
        }

        private void AddTaiChoButton_Click(object sender, RoutedEventArgs e) => OpenHoaDonWithPhanLoai("Tại Chỗ");
        private void AddMuaVeButton_Click(object sender, RoutedEventArgs e) => OpenHoaDonWithPhanLoai("Mv");
        private void AddShipButton_Click(object sender, RoutedEventArgs e) => OpenHoaDonWithPhanLoai("Ship");
        private void AddAppButton_Click(object sender, RoutedEventArgs e) => OpenHoaDonWithPhanLoai("App");

        // ==================== APP SHIPPING POPUP ====================
        private async void AppButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(AppButton, async _ =>
            {
                var helper = new AppShippingHelperText("12122431577", "baothanh1991");
                var dto = await Task.Run(() => helper.GetFirstOrderPopup());

                var owner = Window.GetWindow(this);
                var window = new HoaDonEdit(dto)
                {
                    Owner = owner,
                    Width = this.ActualWidth,
                    Height = this.ActualHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (window.ShowDialog() == true)
                {
                    var savedId = window.SavedHoaDonId ?? window.Model?.Id;

                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);

                    try { SearchHoaDonTextBox?.Clear(); } catch { }

                    if (savedId.HasValue && savedId.Value != Guid.Empty)
                        await SelectHoaDonByIdAsync(savedId.Value);
                    else
                        await SelectNewestHoaDonRowAsync();
                }
            });
        }

        // ==================== SCROLL PANE ====================
        private void ThongTinThanhToanGroupBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sv = sender as ScrollViewer;
            if (sv == null) return;

            double offset = sv.VerticalOffset - Math.Sign(e.Delta) * 48;
            if (offset < 0) offset = 0;
            if (offset > sv.ScrollableHeight) offset = sv.ScrollableHeight;

            sv.ScrollToVerticalOffset(offset);
            e.Handled = true;
        }

        // ==================== ICON TRẠNG THÁI ====================
        private void StatusIcon_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is IconBlock icon && icon.DataContext is HoaDonDto hd)
            {
                ApplyStatusIcon(hd, icon);
            }
        }

        private void ApplyStatusIcon(HoaDonDto hd, IconBlock icon)
        {
            // Reset
            icon.Visibility = Visibility.Collapsed;
            icon.BeginAnimation(UIElement.OpacityProperty, null);
            icon.Opacity = 1;

            if (hd == null || hd.DaThuHoacGhiNo) return;

            icon.Visibility = Visibility.Visible;

            switch (hd.PhanLoai)
            {
                case "App":
                    if (hd.TrangThai == "Chưa thu")
                    {
                        icon.Icon = IconChar.MobileScreenButton;
                        icon.Foreground = (Brush)Application.Current.Resources["DangerBrush"];
                    }
                    break;
                case "Tại Chỗ":
                    if (hd.TrangThai == "Chưa thu")
                    {
                        icon.Icon = IconChar.Chair;
                        icon.Foreground = (Brush)Application.Current.Resources["SuccessBrush"];
                    }
                    break;
                case "Mv":
                    if (hd.TrangThai == "Chưa thu")
                    {
                        icon.Icon = IconChar.BagShopping;
                        icon.Foreground = (Brush)Application.Current.Resources["WarningBrush"];
                    }
                    break;
                case "Ship":
                    icon.Icon = hd.NguoiShip == "Khánh"
                        ? IconChar.Motorcycle
                        : (hd.NgayShip == null ? IconChar.HourglassHalf : IconChar.Truck);

                    icon.Foreground = hd.NguoiShip == "Khánh"
                        ? (Brush)Application.Current.Resources["DangerBrush"]
                        : (hd.NgayShip == null
                            ? (Brush)Application.Current.Resources["PrimaryBrush"]
                            : (Brush)Application.Current.Resources["DarkBrush"]);
                    break;
                default:
                    icon.Icon = IconChar.Circle;
                    icon.Foreground = (Brush)Application.Current.Resources["SecondaryBrush"];
                    break;
            }

            // Blink nếu Ship chưa đi (khớp code gốc)
            var blink = new DoubleAnimation
            {
                From = 1.0,
                To = 0.2,
                Duration = TimeSpan.FromSeconds(0.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            if (hd.PhanLoai == "Ship" && hd.NgayShip == null)
                icon.BeginAnimation(UIElement.OpacityProperty, blink);
        }

        // ==================== CHỌN HĐ -> TẢI CHI TIẾT ====================
        private async void HoaDonDataGrid_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _cts?.Cancel();
                TTSHelper.Stop();

                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                if (HoaDonDataGrid.SelectedItem is HoaDonDto selected)
                {
                    HoaDonDetailPanel.DataContext = selected;
                    await AnimationHelper.FadeSwitchAsync(
                        HoaDonDetailPanel.Visibility == Visibility.Visible ? HoaDonDetailPanel : null,
                        HoaDonDetailPanel);

                    // Reset UI
                    SearchChiTietHoaDonTextBox.Visibility = Visibility.Collapsed;
                    TongSoSanPhamTextBlock.Text = string.Empty;
                    TenHoaDonTextBlock.Text = string.Empty;
                    ChiTietHoaDonListBox.ItemsSource = null;

                    var api = new HoaDonApi();
                    var getResult = await api.GetByIdAsync(selected.Id);
                    if (!getResult.IsSuccess || getResult.Data == null)
                    {
                        NotiHelper.ShowError($"Lỗi: {getResult.Message}");
                        return;
                    }

                    var hd = getResult.Data;

                    // Nếu đang "Báo đơn" thì tắt ngay khi mở chi tiết
                    if (selected.BaoDon == true)
                    {
                        selected.BaoDon = false;
                        var updateResult = await api.UpdateSingleAsync(selected.Id, selected);
                        if (!updateResult.IsSuccess)
                            NotiHelper.ShowError($"Lỗi: {updateResult.Message}");
                        else
                        {
                            await AppProviders.HoaDons.ReloadAsync();
                            ReloadHoaDonUI();

                            var items = HoaDonDataGrid.ItemsSource as IEnumerable<HoaDonDto>;
                            var again = items?.FirstOrDefault(x => x.Id == selected.Id);
                            if (again != null) HoaDonDataGrid.SelectedItem = again;
                        }
                    }

                    // Cập nhật UI chi tiết
                    ChiTietHoaDonListBox.ItemsSource = hd.ChiTietHoaDons;
                    _fullChiTietHoaDonList = hd.ChiTietHoaDons?.ToList() ?? new List<ChiTietHoaDonDto>();

                    UpdateThongTinThanhToanStyle(hd);
                    ThongTinThanhToanPanel.DataContext = hd;
                    RenderFooterPanel(ThongTinThanhToanPanel, hd, includeLine: false);
                    TenHoaDonTextBlock.Text = $"{hd.Ten} - {hd.DiaChiText}";

                    // Tính tổng số sản phẩm theo đúng rule loại trừ nhóm (gốc)
                    try
                    {
                        TongSoSanPhamTextBlock.Text = hd.ChiTietHoaDons
                            .Where(ct =>
                            {
                                var bienThe = AppProviders.SanPhams.Items.SelectMany(sp => sp.BienThe)
                                               .FirstOrDefault(bt => bt.Id == ct.SanPhamIdBienThe);
                                if (bienThe == null) return false;
                                var sp = AppProviders.SanPhams.Items.FirstOrDefault(s => s.Id == bienThe.SanPhamId);
                                if (sp == null) return false;
                                return sp.TenNhomSanPham != "Thuốc lá"
                                    && sp.TenNhomSanPham != "Ăn vặt"
                                    && sp.TenNhomSanPham != "Nước lon";
                            })
                            .Sum(ct => ct.SoLuong)
                            .ToString("N0");
                    }
                    catch { /* fallback im lặng nếu thiếu dữ liệu */ }
                }
                else
                {
                    await AnimationHelper.FadeSwitchAsync(HoaDonDetailPanel, null);
                }
            }
            catch (OperationCanceledException)
            {
                // bị hủy -> bỏ qua
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError($"Lỗi: {ex.Message}");
            }
        }

        // ==================== LỌC CHI TIẾT HĐ (ô search bên phải) ====================
        private void ApplyChiTietHoaDonFilter()
        {
            var text = SearchChiTietHoaDonTextBox.Text?.Trim().ToLower() ?? "";

            var sourceList = _fullChiTietHoaDonList
                .Where(x => string.IsNullOrEmpty(text)
                         || (x.TenSanPham ?? "").ToLower().Contains(text)
                         || (x.TenBienThe ?? "").ToLower().Contains(text)
                         || (x.NoteText ?? "").ToLower().Contains(text))
                .ToList();

            int stt = 1;
            foreach (var item in sourceList) item.Stt = stt++;

            ChiTietHoaDonListBox.ItemsSource = sourceList;
            TongSoSanPhamTextBlock.Text = sourceList.Sum(x => x.SoLuong).ToString("N0");
        }

        private void SearchChiTietHoaDonTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                DebounceSearch(tb, "ChiTietHoaDon", ApplyChiTietHoaDonFilter);
        }

        // ==================== DOUBLE-CLICK SỬA HĐ ====================
        private async void HoaDonDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;
            try
            {
                var api = new HoaDonApi();
                var result = await api.GetByIdAsync(selected.Id);

                if (!result.IsSuccess || result.Data == null)
                {
                    NotiHelper.ShowError($"Không thể tải chi tiết hóa đơn: {result.Message}");
                    return;
                }

                var owner = Window.GetWindow(this);
                var window = new HoaDonEdit(result.Data)
                {
                    Width = this.ActualWidth,
                    Height = this.ActualHeight,
                    Owner = owner
                };
                if (window.ShowDialog() == true)
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError(ex.Message);
            }
        }

        // ==================== SAFE BUTTON WRAPPER ====================
        private async Task SafeButtonHandlerAsync(ButtonBase? button, Func<ButtonBase?, Task> action, Func<bool>? requireSelectedHoaDon = null)
        {
            try
            {
                if (requireSelectedHoaDon != null && !requireSelectedHoaDon())
                {
                    NotiHelper.Show("Vui lòng chọn hoá đơn!");
                    return;
                }

                if (button != null) button.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;
                ProgressBar.Visibility = Visibility.Visible;

                await action(button);
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError(ex.Message);
            }
            finally
            {
                await Task.Delay(100);
                Mouse.OverrideCursor = null;
                ProgressBar.Visibility = Visibility.Collapsed;
                if (button != null) button.IsEnabled = true;
            }
        }

        // ==================== CHỌN DÒNG THEO ID / NEWEST ====================
        private async Task SelectHoaDonByIdAsync(Guid id)
        {
            if (id == Guid.Empty) return;

            var ok = await WaitUntilAsync(() =>
            {
                var items = HoaDonDataGrid?.Items?.OfType<HoaDonDto>();
                return items != null && items.Any(x => x.Id == id);
            });

            if (!ok) return;

            var item = HoaDonDataGrid.Items.OfType<HoaDonDto>().First(x => x.Id == id);

            HoaDonDataGrid.SelectedItem = item;
            HoaDonDataGrid.ScrollIntoView(item);
            HoaDonDataGrid.UpdateLayout();

            await Dispatcher.InvokeAsync(() =>
            {
                var row = (DataGridRow)HoaDonDataGrid.ItemContainerGenerator.ContainerFromItem(item);
                row?.Focus();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private async Task SelectNewestHoaDonRowAsync()
        {
            var ok = await WaitUntilAsync(() =>
            {
                var items = HoaDonDataGrid?.Items?.OfType<HoaDonDto>();
                return items != null && items.Any();
            });

            if (!ok) return;

            var list = HoaDonDataGrid.Items.OfType<HoaDonDto>().ToList();
            var newest = list.OrderByDescending(x => x.NgayGio).FirstOrDefault();
            if (newest == null) return;

            HoaDonDataGrid.SelectedItem = newest;
            HoaDonDataGrid.ScrollIntoView(newest);
            HoaDonDataGrid.UpdateLayout();

            await Dispatcher.InvokeAsync(() =>
            {
                var row = (DataGridRow)HoaDonDataGrid.ItemContainerGenerator.ContainerFromItem(newest);
                row?.Focus();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        // ==================== F1/F2/F3/F4/F5/F6 ====================
        private ChiTietHoaDonThanhToanDto TaoDtoThanhToan(HoaDonDto selected, Guid phuongThucId)
        {
            var now = DateTime.Now;
            var trongngay = now.Date == selected.Ngay;

            return new ChiTietHoaDonThanhToanDto
            {
                Ngay = trongngay ? now.Date : selected.Ngay,
                NgayGio = trongngay ? now : selected.Ngay.AddDays(1).AddMinutes(-1),
                HoaDonId = selected.Id,
                KhachHangId = selected.KhachHangId,
                Ten = $"{selected.Ten}",
                LoaiThanhToan = "Trong ngày",
                PhuongThucThanhToanId = phuongThucId,
                SoTien = selected.ConLai,
            };
        }

        private async void F1Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F1Button, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;

                if (selected.ConLai == 0) { NotiHelper.Show("Hoá đơn đã thu đủ!"); return; }
                if (selected.TrangThai?.ToLower().Contains("nợ") == true) { NotiHelper.Show("Vui lòng thanh toán tại tab Công nợ!"); return; }

                // Tiền mặt
                var dto = TaoDtoThanhToan(selected, Guid.Parse("0121FC04-0469-4908-8B9A-7002F860FB5C"));
                var owner = Window.GetWindow(this);
                var window = new ChiTietHoaDonThanhToanEdit(dto)
                {
                    Owner = owner,
                    Width = owner?.ActualWidth ?? 1200,
                    Height = owner?.ActualHeight ?? 800
                };
                window.PhuongThucThanhToanComboBox.IsEnabled = false;

                if (window.ShowDialog() == true)
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true);
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        private async void F2Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F2Button, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;

                var api = new HoaDonApi();
                var result = await api.GetByIdAsync(selected.Id);

                if (!result.IsSuccess || result.Data == null)
                {
                    NotiHelper.ShowError($"Không thể tải chi tiết hóa đơn: {result.Message}");
                    return;
                }

                HoaDonPrinter.Print(result.Data);
                selected.NgayRa = DateTime.Now;
                await api.UpdateSingleAsync(selected.Id, selected);
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        private async void F3Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F3Button, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;

                var api = new HoaDonApi();
                var result = await api.GetByIdAsync(selected.Id);

                if (!result.IsSuccess || result.Data == null)
                {
                    NotiHelper.ShowError($"Không thể tải chi tiết hóa đơn: {result.Message}");
                    return;
                }

                var _a = HoaDonPrinter.PrepareZaloTextAndQr_AlwaysCopy(result.Data);
                NotiHelper.Show("Đã copy, ctrl+V để gửi!");
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        private async void F4Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F4Button, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;

                if (selected.ConLai == 0) { NotiHelper.Show("Hoá đơn đã thu đủ!"); return; }
                if (selected.TrangThai?.ToLower().Contains("nợ") == true) { NotiHelper.Show("Vui lòng thanh toán tại tab Công nợ!"); return; }

                // Chuyển khoản
                var dto = TaoDtoThanhToan(selected, Guid.Parse("2cf9a88f-3bc0-4d4b-940d-f8ffa4affa02"));
                var owner = Window.GetWindow(this);
                var window = new ChiTietHoaDonThanhToanEdit(dto)
                {
                    Owner = owner,
                    Width = owner?.ActualWidth ?? 1200,
                    Height = owner?.ActualHeight ?? 800
                };
                window.PhuongThucThanhToanComboBox.IsEnabled = false;

                if (window.ShowDialog() == true)
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true);
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        private async void F5Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F5Button, async _ =>
            {
                await AppProviders.HoaDons.ReloadAsync();
                ReloadHoaDonUI();
                NotiHelper.Show("Đã làm mới danh sách hoá đơn.");
            });
        }

        private async void F6Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F6Button, async _ =>
            {
                var response = await ApiClient.GetAsync("/api/Common/SendBankInfo");
                var result = await response.Content.ReadFromJsonAsync<Result<string>>();

                if (result?.IsSuccess == true)
                    NotiHelper.Show("Đã gửi STK.");
                else
                    NotiHelper.ShowError(result?.Message ?? "Không thể gửi STK.");
            });
        }

        // ==================== ESC / F7 / F8 / F9 / F10 / HẸN GIỜ / LỊCH SỬ / XOÁ / GHI NỢ ====================
        private async void EscButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(EscButton, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;

                var confirm = MessageBox.Show(
                    $"Nếu shipper là Khánh chọn YES\nNếu không phải chọn NO\nHuỷ bỏ chọn CANCEL",
                    "QUAN TRỌNG:",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );

                if (confirm == MessageBoxResult.Cancel) return;

                if (confirm == MessageBoxResult.Yes)
                {
                    selected.NguoiShip = "Khánh";
                }
                else // No
                {
                    selected.NguoiShip = null;
                }

                // KHỚP CODE GỐC: khi xác nhận thì set NgayShip = Now
                selected.NgayShip = DateTime.Now;

                var api = new HoaDonApi();
                var result = await api.UpdateSingleAsync(selected.Id, selected);

                if (!result.IsSuccess)
                {
                    NotiHelper.ShowError($"Lỗi: {result.Message}");
                }
                else
                {
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true);
                    // Nếu chưa in bill thì in luôn
                    if (selected.NgayRa == null)
                        F2Button_Click(this, new RoutedEventArgs());
                }
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        private async void F7Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F7Button, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;

                selected.BaoDon = !selected.BaoDon;
                var api = new HoaDonApi();
                var result = await api.UpdateSingleAsync(selected.Id, selected);

                if (!result.IsSuccess)
                    NotiHelper.ShowError($"Lỗi: {result.Message}");
                else
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true);
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        private async void F8Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F8Button, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;

                selected.UuTien = !selected.UuTien;
                var api = new HoaDonApi();
                var result = await api.UpdateSingleAsync(selected.Id, selected);

                if (!result.IsSuccess)
                    NotiHelper.ShowError($"Lỗi: {result.Message}");
                else
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true);
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        private async void F9Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F9Button, async _ =>
            {
                var folderPath = @"C:\DennMenu";

                if (!Directory.Exists(folderPath))
                {
                    NotiHelper.Show("Thư mục không tồn tại!");
                    return;
                }

                var imageFiles = Directory.GetFiles(folderPath, "*.*")
                                          .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                                                   || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                                                   || f.EndsWith(".jfif", StringComparison.OrdinalIgnoreCase)
                                                   || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                                                   || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                                          .ToList();

                if (imageFiles.Count == 0)
                {
                    NotiHelper.Show("Không tìm thấy hình trong thư mục!");
                    return;
                }

                var files = new System.Collections.Specialized.StringCollection();
                foreach (var file in imageFiles)
                    files.Add(file);

                Clipboard.SetFileDropList(files);
                NotiHelper.Show($"Đã copy {files.Count} hình trong thư mục, Ctrl+V để gửi!");
                await Task.CompletedTask;
            });
        }

        private async void F10Button_Click(object sender, RoutedEventArgs e)
        {
            // theo code gốc dùng F9Button làm anchor cho wrapper
            await SafeButtonHandlerAsync(F9Button, async _ =>
            {
                if (HenGioStackPanel.Visibility != Visibility.Visible)
                {
                    // KHỚP GỐC: offset -6h, phút theo bội 10
                    GioCombo.SelectedIndex = Math.Max(0, DateTime.Now.Hour - 6);
                    PhutCombo.SelectedIndex = Math.Max(0, DateTime.Now.Minute / 10);
                    HenGioStackPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    HenGioStackPanel.Visibility = Visibility.Collapsed;
                }
                await Task.CompletedTask;
            });
        }

        private async void OkHenGioButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(OkHenGio, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
                {
                    NotiHelper.Show("Vui lòng chọn hoá đơn!");
                    return;
                }

                int.TryParse(GioCombo.SelectedItem?.ToString(), out int gio);
                int.TryParse(PhutCombo.SelectedItem?.ToString(), out int phut);

                selected.NgayHen = DateTime.Now.Date.AddHours(gio).AddMinutes(phut);

                var api = new HoaDonApi();
                var result = await api.UpdateSingleAsync(selected.Id, selected);

                if (!result.IsSuccess)
                    NotiHelper.ShowError($"Lỗi: {result.Message}");
                else
                {
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true);
                    HenGioStackPanel.Visibility = Visibility.Collapsed;
                }
            });
        }

        private async void HisButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(HisButton, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;

                if (selected.KhachHangId == null)
                {
                    NotiHelper.Show("Hoá đơn ko có thông tin khách hàng!");
                    return;
                }

                var result = await ApiClient.GetAsync($"/api/Dashboard/lichsu-khachhang/{selected.KhachHangId}");
                var dashboard = await result.Content.ReadFromJsonAsync<DashboardDto>();
                if (dashboard != null)
                {
                    ChiTietHoaDonListBox.ItemsSource = dashboard.History ?? new List<ChiTietHoaDonDto>();
                    _fullChiTietHoaDonList = dashboard.History ?? new List<ChiTietHoaDonDto>();
                    TongSoSanPhamTextBlock.Visibility = Visibility.Collapsed;
                    SearchChiTietHoaDonTextBox.Visibility = Visibility.Visible;
                    SearchChiTietHoaDonTextBox.Focus();
                }
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        private async void DelButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(DelButton, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;

                var confirm = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xoá '{selected.Ten}'?",
                    "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                var response = await ApiClient.DeleteAsync($"/api/HoaDon/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<HoaDonDto>>();

                if (result?.IsSuccess == true)
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
                else
                    NotiHelper.ShowError(result?.Message ?? "Không thể xoá.");
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        private async void F12Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F12Button, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;

                if (selected.ConLai == 0) { NotiHelper.Show("Hoá đơn đã thu đủ!"); return; }
                if (selected.TrangThai?.ToLower().Contains("nợ") == true) { NotiHelper.Show("Hoá đơn đã ghi nợ!"); return; }
                if (selected.KhachHangId == null) { NotiHelper.Show("Hoá đơn chưa có thông tin khách hàng!"); return; }

                var now = DateTime.Now;
                var trongngay = now.Date == selected.Ngay;

                var dto = new ChiTietHoaDonNoDto
                {
                    Ngay = trongngay ? now.Date : selected.Ngay,
                    NgayGio = trongngay ? now : selected.Ngay.AddDays(1).AddMinutes(-1),
                    HoaDonId = selected.Id,
                    KhachHangId = selected.KhachHangId,
                    Ten = $"{selected.Ten}",
                    SoTienNo = selected.ConLai,
                    MaHoaDon = selected.MaHoaDon,
                    GhiChu = selected.GhiChu,
                };

                var owner = Window.GetWindow(this);
                var window = new ChiTietHoaDonNoEdit(dto)
                {
                    Owner = owner,
                    Width = owner?.ActualWidth ?? 1200,
                    Height = owner?.ActualHeight ?? 800,
                    Background = MakeBrush((Brush)Application.Current.Resources["DangerBrush"], 0.8)
                };
                window.SoTienTextBox.IsReadOnly = true;

                if (window.ShowDialog() == true)
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadNo: true);
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        // ==================== UI STYLE TT THANH TOÁN & FOOTER ====================
        private void UpdateThongTinThanhToanStyle(HoaDonDto hd)
        {
            // mặc định
            ThongTinThanhToanGroupBox.Background = (Brush)Application.Current.Resources["LightBrush"];
            ThongTinThanhToanGroupBox.Foreground = (Brush)Application.Current.Resources["DarkBrush"];

            // Ưu tiên: nếu còn nợ khách hàng > 0 thì luôn đỏ nhạt và dừng
            if (hd.TongNoKhachHang > 0)
            {
                ThongTinThanhToanGroupBox.Background = (Brush)Application.Current.Resources["DangerBrush"];
                ThongTinThanhToanGroupBox.Foreground = (Brush)Application.Current.Resources["LightBrush"];
                return;
            }

            // Map theo trạng thái như code gốc
            switch (hd.TrangThai)
            {
                case "Tiền mặt":
                    ThongTinThanhToanGroupBox.Background = (Brush)Application.Current.Resources["SuccessBrush"];
                    ThongTinThanhToanGroupBox.Foreground = (Brush)Application.Current.Resources["LightBrush"];
                    break;

                case "Chuyển khoản":
                case "Banking Nhã":
                    ThongTinThanhToanGroupBox.Background = (Brush)Application.Current.Resources["WarningBrush"];
                    ThongTinThanhToanGroupBox.Foreground = (Brush)Application.Current.Resources["DarkBrush"];
                    break;

                case "Chuyển khoản + Tiền mặt":
                    ThongTinThanhToanGroupBox.Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 0),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Colors.LightGreen, 0.0),
                            new GradientStop(Colors.LightGreen, 0.5),
                            new GradientStop(Colors.LightYellow, 0.5),
                            new GradientStop(Colors.LightYellow, 1.0)
                        }
                    };
                    ThongTinThanhToanGroupBox.Foreground = (Brush)Application.Current.Resources["DarkBrush"];
                    break;

                case "Banking Nhã + Tiền mặt":
                    ThongTinThanhToanGroupBox.Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 0),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Colors.LightGreen, 0.0),
                            new GradientStop(Colors.LightGreen, 0.5),
                            new GradientStop(Colors.Gold, 0.5),
                            new GradientStop(Colors.Gold, 1.0)
                        }
                    };
                    ThongTinThanhToanGroupBox.Foreground = (Brush)Application.Current.Resources["DarkBrush"];
                    break;

                case "Thu một phần":
                    ThongTinThanhToanGroupBox.Background = (Brush)Application.Current.Resources["SuccessBrush"];
                    ThongTinThanhToanGroupBox.Foreground = (Brush)Application.Current.Resources["DarkBrush"];
                    break;

                case "Nợ một phần":
                    ThongTinThanhToanGroupBox.Background = (Brush)Application.Current.Resources["DangerBrush"];
                    ThongTinThanhToanGroupBox.Foreground = (Brush)Application.Current.Resources["DarkBrush"]; // đỏ nhạt → chữ đen vẫn đọc được
                    break;

                case "Ghi nợ":
                    ThongTinThanhToanGroupBox.Background = (Brush)Application.Current.Resources["DangerBrush"];
                    ThongTinThanhToanGroupBox.Foreground = (Brush)Application.Current.Resources["LightBrush"];
                    break;
            }
        }

        public static void RenderFooterPanel(StackPanel host, HoaDonDto hd, bool includeLine = true)
        {
            host.Children.Clear();

            void AddGridRow(string left, string right)
            {
                var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // cột số tiền

                var lb = new TextBlock { Text = left, FontSize = 18, FontWeight = FontWeights.Medium };
                var spacer = new TextBlock { Text = " ", FontSize = 18 }; // đẩy cột tiền sang phải
                var rb = new TextBlock { Text = right, FontSize = 18, FontWeight = FontWeights.Medium };

                Grid.SetColumn(lb, 0);
                Grid.SetColumn(spacer, 1);
                Grid.SetColumn(rb, 2);
                g.Children.Add(lb);
                g.Children.Add(spacer);
                g.Children.Add(rb);
                host.Children.Add(g);
            }

            string VND(decimal v) => $"{v:N0} đ";

            if (hd.KhachHangId != null)
            {
                var s1 = StarHelper.GetStarText(hd.DiemThangNay);
                if (!string.IsNullOrEmpty(s1)) AddGridRow("Tháng này:", s1);
                var s2 = StarHelper.GetStarText(hd.DiemThangTruoc);
                if (!string.IsNullOrEmpty(s2)) AddGridRow("Tháng trước:", s2);
            }

            if (includeLine) host.Children.Add(new Separator());

            if (hd.GiamGia > 0)
            {
                AddGridRow("TỔNG CỘNG:", VND(hd.TongTien));
                AddGridRow("Giảm giá:", VND(hd.GiamGia));
                AddGridRow("Thành tiền:", VND(hd.ThanhTien));
            }
            else
            {
                AddGridRow("Thành tiền:", VND(hd.ThanhTien));
            }

            if (hd.DaThu > 0)
            {
                AddGridRow("Đã thu:", VND(hd.DaThu));
                AddGridRow("Còn lại:", VND(hd.ConLai));
            }

            if (hd.TongNoKhachHang > 0)
            {
                if (includeLine) host.Children.Add(new Separator());
                AddGridRow("Công nợ:", VND(hd.TongNoKhachHang));
                AddGridRow("TỔNG:", VND(hd.TongNoKhachHang + hd.ConLai));
            }
        }

        // ==================== SAU KHI LƯU/SỬA HĐ ====================
        private async Task ReloadAfterHoaDonChangeAsync(bool reloadHoaDon = false, bool reloadThanhToan = false, bool reloadNo = false)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (reloadHoaDon) await AppProviders.HoaDons.ReloadAsync();
                if (reloadThanhToan) await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();
                if (reloadNo) await AppProviders.ChiTietHoaDonNos.ReloadAsync();

                ReloadHoaDonUI();
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError(ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        // ==================== BÁO ĐƠN HẸN GIỜ (TIMER TICK) ====================
        private async Task BaoDonTimer_Tick()
        {
            if (DateTime.Now.Second < 5)
            {
                var visible = (HoaDonDataGrid.ItemsSource as IEnumerable<HoaDonDto>) ?? Enumerable.Empty<HoaDonDto>();
                foreach (var item in visible)
                    item.RefreshGioHienThi();
            }

            var now = DateTime.Now;
            var dueBatch = await Task.Run(() =>
                _fullHoaDonList
                    .Where(h => h.NgayHen.HasValue && h.NgayHen.Value <= now)
                    .OrderBy(h => h.NgayHen)
                    .Take(_hoaDonDueBatchSize)
                    .ToList()
            );

            if (dueBatch.Count == 0) return;

            var api = new HoaDonApi();
            foreach (var hd in dueBatch)
            {
                NotiHelper.Show($"⏰ Đến giờ hẹn: {hd.Ten} ({hd.TongTien:N0}đ)");
                hd.NgayHen = null;

                _ = Task.Run(async () =>
                {
                    try { await api.UpdateSingleAsync(hd.Id, hd); }
                    catch { }
                });
            }
        }

        // ==================== HELPERS ====================
        private Brush MakeBrush(Brush baseBrush, double opacity)
        {
            if (baseBrush is SolidColorBrush scb)
            {
                var c = scb.Color;
                return new SolidColorBrush(Color.FromArgb((byte)(Math.Clamp(opacity, 0, 1) * 255), c.R, c.G, c.B));
            }
            var clone = baseBrush.Clone();
            clone.Opacity = opacity;
            return clone;
        }

        private void ChiTietHoaDonListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChiTietHoaDonListBox.SelectedItem is not ChiTietHoaDonDto selected)
            {
                return;
            }

            var sp = AppProviders.SanPhams.Items
                .SingleOrDefault(x => x.Ten == selected.TenSanPham);

            selected.DinhLuong = sp == null ? "" : sp.DinhLuong;
        }

        public void HandleHotkey(Key key)
        {
            switch (key)
            {
                case Key.Escape: EscButton_Click(this, new RoutedEventArgs()); break;
                case Key.F1: F1Button_Click(this, new RoutedEventArgs()); break;
                case Key.F2: F2Button_Click(this, new RoutedEventArgs()); break;
                case Key.F3: F3Button_Click(this, new RoutedEventArgs()); break;
                case Key.F4: F4Button_Click(this, new RoutedEventArgs()); break;
                case Key.F5: F5Button_Click(this, new RoutedEventArgs()); break;
                case Key.F6: F6Button_Click(this, new RoutedEventArgs()); break;
                case Key.F7: F7Button_Click(this, new RoutedEventArgs()); break;
                case Key.F8: F8Button_Click(this, new RoutedEventArgs()); break;
                case Key.F9: F9Button_Click(this, new RoutedEventArgs()); break;
                case Key.F10: F10Button_Click(this, new RoutedEventArgs()); break;
                case Key.F12: F12Button_Click(this, new RoutedEventArgs()); break;
                case Key.Delete: DelButton_Click(this, new RoutedEventArgs()); break;
            }
        }
    }
}
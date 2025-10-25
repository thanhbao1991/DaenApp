using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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

        private ICollectionView? _hoaDonView;
        private bool _suspendSelectionChanged = false;
        private Guid? _selectedIdBeforeRebind = null;

        // Payment methods (giữ nguyên GUID gốc)
        private static readonly Guid PM_TienMat = Guid.Parse("0121FC04-0469-4908-8B9A-7002F860FB5C");
        private static readonly Guid PM_ChuyenKhoan = Guid.Parse("2cf9a88f-3bc0-4d4b-940d-f8ffa4affa02");

        // Sequence vô hiệu hoá kết quả cũ khi user đổi nhanh selection
        private int _selectionSeq = 0;

        // App helper (giữ nguyên hành vi)
        private readonly AppShippingHelperText _appHelper
            = new AppShippingHelperText("12122431577", "baothanh1991");

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

            // Tạo view một lần và áp filter ban đầu
            BuildHoaDonView();
            ApplyHoaDonFilter();

            // Khôi phục selection lần đầu (optional)
            await RestoreSelectionByIdAsync(null);
        }

        // ==================== DEBOUNCE & UTIL ====================
        private void DebounceSearch(TextBox tb, string key, Action applyFilter, int delayMs = 300)
            => _debouncer.Debounce(key, delayMs, applyFilter);

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
        public void ReloadHoaDonUI(Guid? preferId = null, bool restorePreviousIfNoPrefer = true)
        {
            _selectedIdBeforeRebind = (HoaDonDataGrid.SelectedItem as HoaDonDto)?.Id;

            BuildHoaDonView();
            ApplyHoaDonFilter();

            if (preferId.HasValue && preferId.Value != Guid.Empty)
            {
                // Chọn đúng dòng mong muốn và cho phép SelectionChanged chạy
                _ = SelectHoaDonByIdAsync(preferId.Value);
            }
            else if (restorePreviousIfNoPrefer)
            {
                // Chỉ khôi phục dòng cũ khi không có preferId
                _ = RestoreSelectionByIdAsync(_selectedIdBeforeRebind);
            }
        }
        private void BuildHoaDonView()
        {
            _fullHoaDonList = AppProviders.HoaDons.Items
                .Where(x => !x.IsDeleted)
                .Where(x => x.Ngay.Date == today.Date || x.DaThuHoacGhiNo)
                .OrderBy(x =>
                {
                    if (x.UuTien) return 0;
                    if (x.PhanLoai == "Ship" && x.NgayShip == null) return 1;
                    if (x.PhanLoai != "Ship" &&
                       (x.TrangThai == "Chưa thu" || x.TrangThai == "Thu một phần" || x.TrangThai == "Chuyển khoản một phần"))
                        return 2;
                    if (x.PhanLoai == "Ship" && x.NgayShip != null && !x.DaThuHoacGhiNo) return 3;
                    return 4;
                })
                .ThenByDescending(x => x.NgayGio)
                .ToList();

            _hoaDonView = CollectionViewSource.GetDefaultView(_fullHoaDonList);
            _hoaDonView.SortDescriptions.Clear();

            if (HoaDonDataGrid.ItemsSource != _hoaDonView)
                HoaDonDataGrid.ItemsSource = _hoaDonView;
        }

        private void RecomputeSttForCurrentView()
        {
            if (_hoaDonView == null) return;
            int stt = 1;
            foreach (var item in _hoaDonView.Cast<HoaDonDto>())
                item.Stt = stt++;
        }

        private void ApplyHoaDonFilter()
        {
            if (_hoaDonView == null) return;

            string keyword = (SearchHoaDonTextBox.Text ?? "").Trim().ToLowerInvariant();

            _hoaDonView.Filter = obj =>
            {
                if (obj is not HoaDonDto x) return false;
                if (string.IsNullOrWhiteSpace(keyword)) return true;

                var haystack = (x.TimKiem ?? $"{x.Ten} {x.TrangThai} {x.PhanLoai} {x.DiaChiText}")
                                .ToLowerInvariant();
                return haystack.Contains(keyword);
            };

            _hoaDonView.Refresh();
            RecomputeSttForCurrentView();
        }

        // ==================== HANDLERS: TÌM KIẾM LIST ====================
        private void SearchHoaDonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                DebounceSearch(tb, "HoaDon", ApplyHoaDonFilter, 300);
        }

        private void SearchHoaDonTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try { SearchHoaDonTextBox.Height = 32; } catch { }
        }

        // ==================== OPEN/EDIT WINDOW HELPERS ====================
        private async Task<Guid?> OpenHoaDonEditAsync(HoaDonDto dto)
        {
            var owner = Window.GetWindow(this);
            var window = new HoaDonEdit(dto)
            {
                Owner = owner,
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var ok = window.ShowDialog() == true;
            if (!ok) return null;

            // Ưu tiên lấy Id do cửa sổ trả về
            return window.SavedHoaDonId ?? window.Model?.Id ?? dto.Id;
        }

        private async Task ReloadAfterHoaDonChangeAsync(
         bool reloadHoaDon = false, bool reloadThanhToan = false, bool reloadNo = false,
         Guid? preferId = null)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (reloadHoaDon) await AppProviders.HoaDons.ReloadAsync();
                if (reloadThanhToan) await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();
                if (reloadNo) await AppProviders.ChiTietHoaDonNos.ReloadAsync();

                ReloadHoaDonUI(preferId); // <-- dùng preferId ở đây
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
        private async Task ReloadSelectAndScrollAsync(Guid? id)
        {
            await ReloadAfterHoaDonChangeAsync(
                reloadHoaDon: true, reloadThanhToan: true, reloadNo: true,
                preferId: id // <-- quan trọng
            );

            try { SearchHoaDonTextBox?.Clear(); } catch { }
            ScrollToTop();
        }

        // ==================== NEW HĐ THEO PHÂN LOẠI ====================
        private async void OpenHoaDonWithPhanLoai(string phanLoai)
        {
            var dto = new HoaDonDto { PhanLoai = phanLoai };
            var savedId = await OpenHoaDonEditAsync(dto);
            if (savedId.HasValue)
                await ReloadSelectAndScrollAsync(savedId.Value);
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
                // Nhường UI render BusyUI trước
                await System.Windows.Threading.Dispatcher
                    .Yield(System.Windows.Threading.DispatcherPriority.Render);

                // Lấy đơn App nặng → chạy off-UI thread
                var dto = await Task.Run(() => _appHelper.GetFirstOrderPopup());
                if (dto == null) return;

                // Nhường một nhịp nữa trước khi dựng Window (nạp XAML)
                await System.Windows.Threading.Dispatcher
                    .Yield(System.Windows.Threading.DispatcherPriority.Background);

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
                    // Ưu tiên Id do form trả ra; fallback sang Model.Id hoặc dto.Id
                    var savedId = window.SavedHoaDonId ?? window.Model?.Id ?? dto.Id;

                    // Reload → chọn đúng dòng vừa thêm → scroll lên đầu
                    await ReloadSelectAndScrollAsync(savedId);
                }
            }, null, "Đang lấy đơn App...");
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
                ApplyStatusIcon(hd, icon);
        }

        private void ApplyStatusIcon(HoaDonDto hd, IconBlock icon)
        {
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

            // Blink nếu Ship chưa đi
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

        private async Task RestoreSelectionByIdAsync(Guid? id)
        {
            if (!id.HasValue || id == Guid.Empty) return;
            await SelectHoaDonByIdAsync(id.Value);  // <-- để SelectionChanged chạy như thường
        }
        // ==================== SELECTION -> TẢI CHI TIẾT ====================
        private async void HoaDonDataGrid_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            //if (_suspendSelectionChanged) return;
            try
            {
                _cts?.Cancel();
                TTSHelper.Stop();

                int mySeq = Interlocked.Increment(ref _selectionSeq);

                await Task.Delay(120);
                if (mySeq != _selectionSeq) return;

                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
                {
                    await AnimationHelper.FadeSwitchAsync(HoaDonDetailPanel, null);
                    return;
                }

                SetRightBusy(true);
                await System.Windows.Threading.Dispatcher
                    .Yield(System.Windows.Threading.DispatcherPriority.Render);

                var api = new HoaDonApi();
                var getResult = await api.GetByIdAsync(selected.Id);

                if (mySeq != _selectionSeq) { SetRightBusy(false); return; }

                if (!getResult.IsSuccess || getResult.Data == null)
                {
                    SetRightBusy(false);
                    NotiHelper.ShowError($"Lỗi: {getResult.Message}");
                    return;
                }

                var hd = getResult.Data;

                // Nếu đang "Báo đơn" thì tắt (giữ sequence)
                if (selected.BaoDon == true)
                {
                    selected.BaoDon = false;
                    var updateResult = await api.UpdateSingleAsync(selected.Id, selected);
                    if (!updateResult.IsSuccess)
                    {
                        SetRightBusy(false);
                        NotiHelper.ShowError($"Lỗi: {updateResult.Message}");
                        return;
                    }

                    await AppProviders.HoaDons.ReloadAsync();
                    ReloadHoaDonUI();

                    if (mySeq == _selectionSeq)
                    {
                        var items = HoaDonDataGrid.ItemsSource as IEnumerable<HoaDonDto>;
                        var again = items?.FirstOrDefault(x => x.Id == selected.Id);
                        if (again != null) HoaDonDataGrid.SelectedItem = again;
                    }
                }

                if (mySeq != _selectionSeq) { SetRightBusy(false); return; }

                HoaDonDetailPanel.DataContext = selected; // header
                ChiTietHoaDonListBox.ItemsSource = hd.ChiTietHoaDons;
                _fullChiTietHoaDonList = hd.ChiTietHoaDons?.ToList() ?? new List<ChiTietHoaDonDto>();

                UpdateThongTinThanhToanStyle(hd);
                ThongTinThanhToanPanel.DataContext = hd;
                RenderFooterPanel(ThongTinThanhToanPanel, hd, includeLine: false);
                TenHoaDonTextBlock.Text = $"{hd.TenHienThi} - {hd.DiaChiText}";

                // Tổng số sp (exclude các nhóm)
                try
                {
                    var excluded = new HashSet<string>(new[] { "Thuốc lá", "Ăn vặt", "Nước lon" });
                    var bienTheLookup = AppProviders.SanPhams.Items
                                           .SelectMany(sp => sp.BienThe.Select(bt => (bt.Id, sp.TenNhomSanPham)))
                                           .ToDictionary(x => x.Id, x => x.TenNhomSanPham);

                    int sum = hd.ChiTietHoaDons
                        .Where(ct =>
                        {
                            if (!bienTheLookup.TryGetValue(ct.SanPhamIdBienThe, out var group)) return false;
                            return !excluded.Contains(group);
                        })
                        .Sum(ct => ct.SoLuong);

                    TongSoSanPhamTextBlock.Text = sum.ToString("N0");
                    TongSoSanPhamTextBlock.Visibility = Visibility.Visible;
                    SearchChiTietHoaDonTextBox.Visibility = Visibility.Collapsed;
                }
                catch { /* ignore */ }

                await AnimationHelper.FadeSwitchAsync(
                    HoaDonDetailPanel.Visibility == Visibility.Visible ? HoaDonDetailPanel : null,
                    HoaDonDetailPanel);
            }
            catch (Exception ex) { NotiHelper.ShowError($"Lỗi: {ex.Message}"); }
            finally
            {
                SetRightBusy(false);
            }
        }

        private void SetRightBusy(bool isBusy)
        {
            try
            {
                RightBusyOverlay.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
                HoaDonDetailPanel.Opacity = isBusy ? 0.5 : 1.0;
                Mouse.OverrideCursor = isBusy ? Cursors.AppStarting : null;
            }
            catch { }
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

                var savedId = await OpenHoaDonEditAsync(result.Data);
                if (savedId.HasValue)
                {
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
                    await SelectHoaDonByIdAsync(savedId.Value);
                    ScrollToTop();
                }
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError(ex.Message);
            }
        }

        // ==================== SAFE BUTTON WRAPPER ====================
        private async Task SafeButtonHandlerAsync(
            ButtonBase? button,
            Func<ButtonBase?, Task> action,
            Func<bool>? requireSelectedHoaDon = null,
            string? busyText = null)
        {
            try
            {
                if (requireSelectedHoaDon != null && !requireSelectedHoaDon())
                {
                    NotiHelper.Show("Vui lòng chọn hoá đơn!");
                    return;
                }

                using (BusyUI.Scope(this, button as Button, busyText ?? "Đang xử lý..."))
                {
                    // Cho UI render spinner trước
                    await System.Windows.Threading.Dispatcher
                        .Yield(System.Windows.Threading.DispatcherPriority.Background);
                    await action(button);
                }
            }
            catch (OperationCanceledException)
            {
                // bị hủy -> bỏ qua
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError(ex.Message);
            }
        }

        // ==================== SELECT BY ID / NEWEST ====================
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

        private void ScrollToTop()
        {
            try
            {
                if (HoaDonDataGrid?.Items?.Count > 0)
                {
                    var first = HoaDonDataGrid.Items[0];
                    HoaDonDataGrid.ScrollIntoView(first);
                    HoaDonDataGrid.UpdateLayout();
                }
            }
            catch { /* ignore */ }
        }

        // ==================== DÙNG CHUNG: TẠO DTO THU TIỀN / GHI NỢ ====================
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

        // ==================== DÙNG CHUNG: PAY FLOW (F1/F4) ====================
        private async Task PayAsync(Guid methodId, ButtonBase trigger)
        {
            await SafeButtonHandlerAsync(trigger, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selectedAtClick) return;

                if (selectedAtClick.ConLai == 0) { NotiHelper.Show("Hoá đơn đã thu đủ!"); return; }
                if (selectedAtClick.TrangThai?.ToLower().Contains("nợ") == true)
                {
                    NotiHelper.Show("Vui lòng thanh toán tại tab Công nợ!");
                    return;
                }

                var idAtClick = selectedAtClick.Id;

                var dto = TaoDtoThanhToan(selectedAtClick, methodId);
                var owner = Window.GetWindow(this);
                var window = new ChiTietHoaDonThanhToanEdit(dto)
                {
                    Owner = owner,
                    Width = owner?.ActualWidth ?? 1200,
                    Height = owner?.ActualHeight ?? 800
                };
                window.PhuongThucThanhToanComboBox.IsEnabled = false;

                if (window.ShowDialog() == true)
                {
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true);
                    await SelectHoaDonByIdAsync(idAtClick);
                    ScrollToTop();
                }
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        // ==================== F1 – Tiền mặt ====================
        private async void F1Button_Click(object sender, RoutedEventArgs e) => await PayAsync(PM_TienMat, F1Button);

        // ==================== F4 – Chuyển khoản ====================
        private async void F4Button_Click(object sender, RoutedEventArgs e) => await PayAsync(PM_ChuyenKhoan, F4Button);

        // ==================== F12 – Ghi nợ ====================
        private async void F12Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F12Button, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selectedAtClick) return;

                if (selectedAtClick.ConLai == 0) { NotiHelper.Show("Hoá đơn đã thu đủ!"); return; }
                if (selectedAtClick.TrangThai?.ToLower().Contains("nợ") == true) { NotiHelper.Show("Hoá đơn đã ghi nợ!"); return; }
                if (selectedAtClick.KhachHangId == null) { NotiHelper.Show("Hoá đơn chưa có thông tin khách hàng!"); return; }

                var idAtClick = selectedAtClick.Id;

                var now = DateTime.Now;
                var trongngay = now.Date == selectedAtClick.Ngay;

                var dto = new ChiTietHoaDonNoDto
                {
                    Ngay = trongngay ? now.Date : selectedAtClick.Ngay,
                    NgayGio = trongngay ? now : selectedAtClick.Ngay.AddDays(1).AddMinutes(-1),
                    HoaDonId = selectedAtClick.Id,
                    KhachHangId = selectedAtClick.KhachHangId,
                    Ten = $"{selectedAtClick.Ten}",
                    SoTienNo = selectedAtClick.ConLai,
                    MaHoaDon = selectedAtClick.MaHoaDon,
                    GhiChu = selectedAtClick.GhiChu,
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
                {
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadNo: true);
                    await SelectHoaDonByIdAsync(idAtClick);
                    ScrollToTop();
                }
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        // ==================== TOGGLE FIELD (F7/F8) DÙNG CHUNG ====================
        private async Task ToggleAndReloadAsync(ButtonBase trigger, Action<HoaDonDto> toggler)
        {
            await SafeButtonHandlerAsync(trigger, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selectedAtClick) return;
                var idAtClick = selectedAtClick.Id;

                toggler(selectedAtClick);
                var api = new HoaDonApi();
                var result = await api.UpdateSingleAsync(selectedAtClick.Id, selectedAtClick);

                if (!result.IsSuccess)
                {
                    NotiHelper.ShowError($"Lỗi: {result.Message}");
                    return;
                }

                await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true);
                await SelectHoaDonByIdAsync(idAtClick);
                ScrollToTop();
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        // ==================== F7 – Báo đơn ====================
        private async void F7Button_Click(object sender, RoutedEventArgs e)
            => await ToggleAndReloadAsync(F7Button, hd => hd.BaoDon = !hd.BaoDon);

        // ==================== F8 – Ưu tiên ====================
        private async void F8Button_Click(object sender, RoutedEventArgs e)
            => await ToggleAndReloadAsync(F8Button, hd => hd.UuTien = !hd.UuTien);

        // ==================== Esc – Gán ship + auto in ====================
        private async void EscButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(EscButton, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selectedAtClick) return;

                var idAtClick = selectedAtClick.Id;

                var confirm = MessageBox.Show(
                    $"Nếu shipper là Khánh chọn YES\nNếu không phải chọn NO\nHuỷ bỏ chọn CANCEL",
                    "QUAN TRỌNG:",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );
                if (confirm == MessageBoxResult.Cancel) return;

                selectedAtClick.NguoiShip = (confirm == MessageBoxResult.Yes) ? "Khánh" : null;
                selectedAtClick.NgayShip = DateTime.Now;

                var api = new HoaDonApi();
                var update = await api.UpdateSingleAsync(idAtClick, selectedAtClick);

                if (!update.IsSuccess)
                {
                    NotiHelper.ShowError($"Lỗi: {update.Message}");
                    return;
                }

                await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true);
                await SelectHoaDonByIdAsync(idAtClick);
                ScrollToTop();

                // Auto in nếu chưa in
                if (selectedAtClick.NgayRa == null)
                {
                    var r2 = await api.GetByIdAsync(idAtClick);
                    if (r2.IsSuccess && r2.Data != null)
                    {
                        HoaDonPrinter.Print(r2.Data);
                        selectedAtClick.NgayRa = DateTime.Now;
                        await api.UpdateSingleAsync(idAtClick, selectedAtClick);
                    }
                }
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        // ==================== Del – Xoá (giữ vị trí lân cận + ScrollToTop) ====================
        private async void DelButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(DelButton, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selectedAtClick) return;

                int oldIndex = HoaDonDataGrid.SelectedIndex;

                var confirm = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xoá '{selectedAtClick.Ten}'?",
                    "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                var response = await ApiClient.DeleteAsync($"/api/HoaDon/{selectedAtClick.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<HoaDonDto>>();

                if (result?.IsSuccess == true)
                {
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);

                    var ok = await WaitUntilAsync(() => HoaDonDataGrid.Items.Count >= 0);
                    if (!ok || HoaDonDataGrid.Items.Count == 0)
                    {
                        ScrollToTop();
                        return;
                    }

                    int newIndex = Math.Min(oldIndex, HoaDonDataGrid.Items.Count - 1);
                    if (newIndex < 0) newIndex = 0;

                    HoaDonDataGrid.SelectedIndex = newIndex;
                    var item = HoaDonDataGrid.Items[newIndex];
                    HoaDonDataGrid.ScrollIntoView(item);
                    HoaDonDataGrid.UpdateLayout();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        var row = (DataGridRow)HoaDonDataGrid.ItemContainerGenerator.ContainerFromIndex(newIndex);
                        row?.Focus();
                    }, System.Windows.Threading.DispatcherPriority.Background);

                    ScrollToTop();
                }
                else
                {
                    NotiHelper.ShowError(result?.Message ?? "Không thể xoá.");
                }
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        // ==================== F2 / F3 / F5 / F6 / F9 / F10 / HẸN GIỜ / LỊCH SỬ ====================
        private async void F2Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F2Button, async _ =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selectedAtClick) return;
                var idAtClick = selectedAtClick.Id;

                var api = new HoaDonApi();
                var result = await api.GetByIdAsync(idAtClick);

                if (!result.IsSuccess || result.Data == null)
                {
                    NotiHelper.ShowError($"Không thể tải chi tiết hóa đơn: {result.Message}");
                    return;
                }

                HoaDonPrinter.Print(result.Data);

                selectedAtClick.NgayRa = DateTime.Now;
                await api.UpdateSingleAsync(idAtClick, selectedAtClick);
            }, () => HoaDonDataGrid.SelectedItem is HoaDonDto);
        }

        private async void F3Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F3Button, async btn =>
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selectedAtClick) return;
                var idAtClick = selectedAtClick.Id;

                var api = new HoaDonApi();
                var result = await api.GetByIdAsync(idAtClick);
                if (!result.IsSuccess || result.Data == null)
                {
                    NotiHelper.ShowError($"Không thể tải chi tiết hóa đơn: {result.Message}");
                    return;
                }

                _ = HoaDonPrinter.PrepareZaloTextAndQr_AlwaysCopy(result.Data);
                NotiHelper.Show("Đã copy, ctrl+V để gửi!");
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
                    // Offset -6h, phút bội 10
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

        // ==================== UI STYLE TT THANH TOÁN & FOOTER ====================
        private void UpdateThongTinThanhToanStyle(HoaDonDto hd)
        {
            ThongTinThanhToanGroupBox.Background = (Brush)Application.Current.Resources["LightBrush"];
            ThongTinThanhToanGroupBox.Foreground = (Brush)Application.Current.Resources["DarkBrush"];

            if (hd.TongNoKhachHang > 0)
            {
                ThongTinThanhToanGroupBox.Background = (Brush)Application.Current.Resources["DangerBrush"];
                ThongTinThanhToanGroupBox.Foreground = (Brush)Application.Current.Resources["LightBrush"];
                return;
            }

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
                    ThongTinThanhToanGroupBox.Foreground = (Brush)Application.Current.Resources["DarkBrush"];
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
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var lb = new TextBlock { Text = left, FontSize = 18, FontWeight = FontWeights.Medium };
                var spacer = new TextBlock { Text = " ", FontSize = 18 };
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

        // ==================== TIMER BÁO ĐƠN HẸN GIỜ ====================
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

        // ==================== HELPERS UI NHỎ ====================
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
            if (ChiTietHoaDonListBox.SelectedItem is not ChiTietHoaDonDto selected) return;

            var sp = AppProviders.SanPhams.Items
                .SingleOrDefault(x => x.Ten == selected.TenSanPham);

            selected.DinhLuong = sp == null ? "" : sp.DinhLuong;
        }

        // ==================== HOTKEYS ====================
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
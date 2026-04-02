using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TraSuaApp.Shared.Constants;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class HoaDonTab : UserControl
    {
        private readonly DebounceManager _debouncer = new();
        private CancellationTokenSource? _ttsCts;
        private CancellationTokenSource? _selectionCts;
        private CancellationTokenSource? _actionCts;

        private List<ChiTietHoaDonDto> _fullChiTietHoaDonList = new();
        private Dictionary<Guid, string>? _bienTheLookup;
        private readonly DashboardApi _api = new();
        private readonly HoaDonApi _hoaDonApi = new();
        private ObservableCollection<HoaDonNoDto> _items = new();
        private ICollectionView? _view;
        private HoaDonDto? _currentHoaDon;

        // ── Cache có giới hạn ──────────────────────────────────────────────────
        private const int MaxCacheSize = 100;
        private readonly Dictionary<Guid, HoaDonDto> _hoaDonCache = new();
        private readonly Queue<Guid> _cacheOrder = new();

        // ── Signal queue thread-safe ───────────────────────────────────────────
        private readonly ConcurrentQueue<Guid> _signalQueue = new();
        private readonly SemaphoreSlim _signalSemaphore = new(1, 1);

        // ── Batch refresh ──────────────────────────────────────────────────────
        private bool _refreshPending = false;

        public HoaDonTab()
        {
            InitializeComponent();
            Loaded += HoaDonTab_Loaded;
            Unloaded += OnUnloaded;
        }

        // ══════════════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ══════════════════════════════════════════════════════════════════════

        private void HoaDonTab_Loaded(object? sender, RoutedEventArgs e)
        {
            StartWaitingTimer();
            BuildBienTheLookup();
            _view = CollectionViewSource.GetDefaultView(_items);
            HoaDonDataGrid.ItemsSource = _view;
            _view.Filter = FilterHoaDon;

            _view.SortDescriptions.Clear();
            _view.SortDescriptions.Add(new SortDescription(nameof(HoaDonNoDto.SortOrder), ListSortDirection.Ascending));
            _view.SortDescriptions.Add(new SortDescription(nameof(HoaDonNoDto.NgayGio), ListSortDirection.Descending));

            ApplyFilter();

            if (_items.Count == 0)
                ReloadAndRestoreSelectionAsync();
        }
        private void BuildBienTheLookup()
        {
            _bienTheLookup = AppProviders.SanPhamBienThes.Items
                .Where(x => x.Id != Guid.Empty)
                .ToDictionary(x => x.Id, x => x.Ten ?? "");
        }
        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            _ttsCts?.Cancel();
            _ttsCts?.Dispose();
            _ttsCts = null;

            _actionCts?.Cancel();
            _actionCts?.Dispose();
            _actionCts = null;
        }

        // ══════════════════════════════════════════════════════════════════════
        // BATCH REFRESH
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gom nhiều lần gọi Refresh trong cùng 1 frame thành 1 lần duy nhất.
        /// </summary>
        private void RequestRefresh()
        {
            if (_refreshPending) return;
            _refreshPending = true;

            Dispatcher.InvokeAsync(() =>
            {
                _refreshPending = false;
                _view?.Refresh();
                ThanhTienColumn.Header = $"{_items.Sum(x => x.ThanhTien):N0}";
            }, DispatcherPriority.Background);
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private void Debounce(string key, Action action, int delay = 300)
            => _debouncer.Debounce(key, delay, action);

        private void SetRightBusy(bool busy)
        {
            RightBusyOverlay.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            HoaDonDetailPanel.Opacity = 1;
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
            catch { }
        }

        private void SelectRow(object sender)
        {
            if (sender is FrameworkElement fe && fe.DataContext is HoaDonNoDto row)
                HoaDonDataGrid.SelectedItem = row;
        }

        // ── Cache helpers ──────────────────────────────────────────────────────

        private void CacheHoaDon(HoaDonDto hd)
        {
            if (_hoaDonCache.ContainsKey(hd.Id))
            {
                _hoaDonCache[hd.Id] = hd;
                return;
            }

            if (_hoaDonCache.Count >= MaxCacheSize && _cacheOrder.TryDequeue(out var oldest))
                _hoaDonCache.Remove(oldest);

            _hoaDonCache[hd.Id] = hd;
            _cacheOrder.Enqueue(hd.Id);
        }

        private void InvalidateCache(Guid id) => _hoaDonCache.Remove(id);

        // ── DTO mapping dùng chung ─────────────────────────────────────────────

        private static HoaDonNoDto ToNoDto(HoaDonDto hd) => new()
        {
            Id = hd.Id,
            TenKhachHangText = string.IsNullOrEmpty(hd.TenKhachHangText) ? hd.TenBan : hd.TenKhachHangText,
            KhachHangId = hd.KhachHangId,
            VoucherId = hd.VoucherId,
            ThanhTien = hd.ThanhTien,
            ConLai = hd.ConLai,
            DaThu = hd.DaThu,
            NgayGio = hd.NgayGio,
            NgayShip = hd.NgayShip,
            NgayNo = hd.NgayNo,
            NgayIn = hd.NgayIn,
            NguoiShip = hd.NguoiShip,
            GhiChu = hd.GhiChu,
            GhiChuShipper = hd.GhiChuShipper,
            PhanLoai = hd.PhanLoai,
            LastModified = hd.LastModified
        };

        private static void PatchNoDto(HoaDonNoDto target, HoaDonNoDto src)
        {
            target.TenKhachHangText = src.TenKhachHangText;
            target.KhachHangId = src.KhachHangId;
            target.VoucherId = src.VoucherId;
            target.ThanhTien = src.ThanhTien;
            target.ConLai = src.ConLai;
            target.DaThu = src.DaThu;
            target.NgayGio = src.NgayGio;
            target.NgayShip = src.NgayShip;
            target.NgayNo = src.NgayNo;
            target.NgayIn = src.NgayIn;
            target.NguoiShip = src.NguoiShip;
            target.GhiChu = src.GhiChu;
            target.GhiChuShipper = src.GhiChuShipper;
            target.IsBank = src.IsBank;
            target.PhanLoai = src.PhanLoai;
            target.Stt = src.Stt;
            target.LastModified = src.LastModified;
        }

        private void UpdateLocalHoaDon(HoaDonNoDto? dto)
        {
            if (dto == null) return;
            var item = _items.FirstOrDefault(x => x.Id == dto.Id);
            if (item == null) return;

            // Invalidate cache vì data local đã thay đổi
            InvalidateCache(dto.Id);

            PatchNoDto(item, dto);
            item.RefreshWaitingTime();
            RequestRefresh();
        }

        private void UpdateOrInsertLocal(HoaDonDto hd)
        {
            var noDto = ToNoDto(hd);
            var item = _items.FirstOrDefault(x => x.Id == hd.Id);

            if (item != null)
            {
                InvalidateCache(hd.Id);
                PatchNoDto(item, noDto);
                item.RefreshWaitingTime();
            }
            else
            {
                _items.Add(noDto);
            }

            RequestRefresh();
        }

        private void RestoreItem(HoaDonNoDto item, int index)
        {
            if (_items.Any(x => x.Id == item.Id)) return;
            if (index >= 0 && index <= _items.Count) _items.Insert(index, item);
            else _items.Add(item);
            RequestRefresh();
        }

        // ══════════════════════════════════════════════════════════════════════
        // SAFE BUTTON HANDLER  — có CancellationToken
        // ══════════════════════════════════════════════════════════════════════

        private async Task SafeButtonHandlerAsync(
            ButtonBase? button,
            Func<CancellationToken, Task> action,
            Func<bool>? requireSelected = null,
            string? busyText = null)
        {
            // Cancel action đang chạy trước đó (nếu có)
            _actionCts?.Cancel();
            _actionCts?.Dispose();
            _actionCts = new CancellationTokenSource();
            var token = _actionCts.Token;

            try
            {
                if (requireSelected != null && !requireSelected())
                {
                    NotiHelper.Show("Vui lòng chọn hoá đơn!");
                    return;
                }

                using (BusyUI.Scope(this, button as Button, busyText ?? "Đang xử lý..."))
                {
                    await Dispatcher.Yield(DispatcherPriority.Background);
                    await action(token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { NotiHelper.ShowError(ex.Message); }
        }


        private Task ExecuteHoaDonActionAsync(
            Button button,
            Func<CancellationToken, Task> action,
            Func<bool>? condition = null)
            => SafeButtonHandlerAsync(button, action, condition ?? (() => true));

        private Task ExecuteHoaDonActionAsync(
            Button button,
            Func<Task> action,
            Func<bool>? condition = null)
            => SafeButtonHandlerAsync(button, _ => action(), condition ?? (() => true));

        // ══════════════════════════════════════════════════════════════════════
        // TTS
        // ══════════════════════════════════════════════════════════════════════

        private void StartReadMonNotes(HoaDonDto hd)
        {
            try
            {
                _ttsCts?.Cancel();

                var noteItems = hd.ChiTietHoaDons?
                    .Where(x => !string.IsNullOrWhiteSpace(x.NoteText))
                    .ToList();

                if (noteItems == null || noteItems.Count == 0) return;

                _ttsCts = new CancellationTokenSource();
                var token = _ttsCts.Token;

                Task.Run(async () =>
                {
                    await Task.Delay(300, token);

                    if (noteItems.Count <= 3)
                    {
                        foreach (var item in noteItems)
                        {
                            if (token.IsCancellationRequested) break;
                            await TTSHelper.DownloadAndPlayGoogleTTSAsync($"{item.TenSanPham}. {item.NoteText}");
                            await Task.Delay(400, token);
                        }
                    }
                    else
                    {
                        var tenKhach = string.IsNullOrWhiteSpace(hd.TenKhachHangText) ? "Khách lẻ" : hd.TenKhachHangText;
                        var tongLy = hd.ChiTietHoaDons?.Sum(x => x.SoLuong) ?? 0;
                        var soLyCoNote = noteItems.Sum(x => x.SoLuong);
                        await TTSHelper.DownloadAndPlayGoogleTTSAsync(
                            $"{tenKhach}, {tongLy} ly, {soLyCoNote} ghi chú");
                    }
                }, token);
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════════════════
        // RENDER FOOTER
        // ══════════════════════════════════════════════════════════════════════

        public void RenderFooterPanel(StackPanel host, HoaDonDto hd, bool includeLine = true)
        {
            host.Children.Clear();

            void AddRow(string left, string right)
            {
                var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var lb = new TextBlock { Text = left, FontSize = 18, FontWeight = FontWeights.Medium };
                var spacer = new TextBlock { Text = " ", FontSize = 18 };
                var rb = new TextBlock { Text = right, FontSize = 18, FontWeight = FontWeights.Medium };

                Grid.SetColumn(lb, 0);
                Grid.SetColumn(spacer, 1);   // FIX: spacer phải ở column 1
                Grid.SetColumn(rb, 2);

                g.Children.Add(lb);
                g.Children.Add(spacer);
                g.Children.Add(rb);
                host.Children.Add(g);
            }

            string VND(decimal v) => $"{v:N0}";

            if (hd.KhachHangId != null)
            {
                DiemThangNayTextBlock.Text = StarHelper.GetStarText(hd.DiemThangNay);
                DiemThangTruocTextBlock.Text = StarHelper.GetStarText(hd.DiemThangTruoc);
            }

            if (includeLine) host.Children.Add(new Separator());

            if (hd.GiamGia > 0)
            {
                AddRow("TỔNG CỘNG:", VND(hd.TongTien));
                AddRow("Giảm giá:", VND(hd.GiamGia));
                AddRow("Thành tiền:", VND(hd.ThanhTien));
            }
            else
            {
                AddRow("Thành tiền:", VND(hd.ThanhTien));
            }

            if (hd.DaThu > 0)
            {
                AddRow("Đã thu:", VND(hd.DaThu));
                AddRow("Còn lại:", VND(hd.ConLai));
            }

            if (hd.TongNoKhachHang > 0)
            {
                if (includeLine) host.Children.Add(new Separator());
                AddRow("Công nợ:", VND(hd.TongNoKhachHang));
                AddRow("TỔNG:", VND(hd.TongNoKhachHang + hd.ConLai));
            }

            if (hd.TongDonKhacDangGiao > 0)
            {
                if (includeLine) host.Children.Add(new Separator());
                AddRow("Đơn khác:", VND(hd.TongDonKhacDangGiao));
                AddRow("TỔNG:", VND(hd.TongNoKhachHang + hd.ConLai + hd.TongDonKhacDangGiao));
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // FILTER / SEARCH
        // ══════════════════════════════════════════════════════════════════════

        private void SearchHoaDonTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => Debounce("HoaDonSearch", ApplyFilter);

        private void ApplyFilter() => RequestRefresh();

        private bool FilterHoaDon(object obj)
        {
            if (obj is not HoaDonNoDto x) return false;

            if (Dashboard.IsThanhToanHidden)
            {
                var cutoff = DateTime.Now.AddHours(-3);
                if (!(x.PhanLoai == "App"
                    || x.LastModified >= cutoff
                    || (x.NgayNo != null && x.IsBank == true)
                    || x.IsBank == true
                    || x.NgayIn != null
                    || (x.IsBank == false && x.NgayGio?.Second % 59 == 0)))
                    return false;
            }

            var keyword = StringHelper.MyNormalizeText(SearchHoaDonTextBox.Text ?? "");
            if (!string.IsNullOrWhiteSpace(keyword))
                if (!(x.TimKiem ?? "").Contains(keyword)) return false;

            return true;
        }

        // ══════════════════════════════════════════════════════════════════════
        // SCROLL
        // ══════════════════════════════════════════════════════════════════════

        private void ThongTinThanhToanGroupBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;
            var offset = Math.Clamp(sv.VerticalOffset - Math.Sign(e.Delta) * 48, 0, sv.ScrollableHeight);
            sv.ScrollToVerticalOffset(offset);
            e.Handled = true;
        }

        // ══════════════════════════════════════════════════════════════════════
        // DATA — API
        // ══════════════════════════════════════════════════════════════════════

        private HoaDonNoDto? SelectedNoHoaDon => HoaDonDataGrid.SelectedItem as HoaDonNoDto;

        private async Task<HoaDonDto?> GetHoaDonAsync(Guid id, CancellationToken ct = default)
        {
            var result = await _hoaDonApi.GetByIdAsync(id, ct);
            if (!result.IsSuccess || result.Data == null)
            {
                NotiHelper.ShowError(result.Message);
                return null;
            }
            return result.Data;
        }

        // ══════════════════════════════════════════════════════════════════════
        // RELOAD
        // ══════════════════════════════════════════════════════════════════════

        public async Task ReloadAndRestoreSelectionAsync(Guid? preferId = null)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                //await Task.WhenAll(AppProviders.ChiTietHoaDonThanhToans.ReloadAsync());

                var response = await _api.GetHoaDon();
                if (!response.IsSuccess) { NotiHelper.ShowError(response.Message); return; }

                var newData = response.Data ?? new List<HoaDonNoDto>();
                var dict = _items.ToDictionary(x => x.Id);

                foreach (var newItem in newData)
                {
                    if (dict.TryGetValue(newItem.Id, out var old))
                    {
                        PatchNoDto(old, newItem);
                        old.RefreshWaitingTime();
                    }
                    else
                    {
                        _items.Add(newItem);
                    }
                }

                var newIds = new HashSet<Guid>(newData.Select(x => x.Id));
                for (int i = _items.Count - 1; i >= 0; i--)
                    if (!newIds.Contains(_items[i].Id))
                        _items.RemoveAt(i);

                RequestRefresh();

                if (preferId.HasValue)
                {
                    var found = _items.FirstOrDefault(x => x.Id == preferId.Value);
                    if (found != null) { HoaDonDataGrid.SelectedItem = found; HoaDonDataGrid.ScrollIntoView(found); }
                    else { HoaDonDataGrid.SelectedItem = null; ScrollToTop(); }
                }
                else
                {
                    HoaDonDataGrid.SelectedItem = null;
                    ScrollToTop();
                }
            }
            catch (Exception ex) { NotiHelper.ShowError(ex.Message); }
            finally { Mouse.OverrideCursor = null; }
        }

        // ══════════════════════════════════════════════════════════════════════
        // SELECTION CHANGED
        // ══════════════════════════════════════════════════════════════════════

        private async void HoaDonDataGrid_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            _selectionCts?.Cancel();
            _selectionCts = new CancellationTokenSource();
            var token = _selectionCts.Token;

            try
            {
                TTSHelper.Stop();

                if (HoaDonDataGrid.SelectedItem is not HoaDonNoDto row)
                {
                    _currentHoaDon = null;
                    await AnimationHelper.FadeSwitchAsync(HoaDonDetailPanel, null);
                    return;
                }

                SetRightBusy(true);

                var hd = await FetchHoaDonForSelectionAsync(row, token);
                if (hd == null || token.IsCancellationRequested) return;

                if (HoaDonDataGrid.SelectedItem is not HoaDonNoDto current || current.Id != row.Id) return;

                BindDetailUI(hd);

                _ = Task.Run(() => StartReadMonNotes(hd));

                await AnimationHelper.FadeSwitchAsync(
                    HoaDonDetailPanel.Visibility == Visibility.Visible ? HoaDonDetailPanel : null,
                    HoaDonDetailPanel);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { NotiHelper.ShowError($"Lỗi: {ex.Message}"); }
            finally { if (!token.IsCancellationRequested) SetRightBusy(false); }
        }

        private async Task<HoaDonDto?> FetchHoaDonForSelectionAsync(HoaDonNoDto row, CancellationToken token)
        {
            // cache hit — chỉ dùng nếu LastModified khớp
            if (_hoaDonCache.TryGetValue(row.Id, out var cached) && cached.LastModified == row.LastModified)
                return cached;

            // cache miss — fetch với cancel token
            var result = await _hoaDonApi.GetByIdAsync(row.Id, token);
            if (token.IsCancellationRequested) return null;

            if (!result.IsSuccess || result.Data == null)
            {
                NotiHelper.ShowError($"Lỗi: {result.Message}");
                return null;
            }

            CacheHoaDon(result.Data);
            return result.Data;
        }

        private void BindDetailUI(HoaDonDto hd)
        {
            _currentHoaDon = hd;

            var chiTiet = hd.ChiTietHoaDons ?? new ObservableCollection<ChiTietHoaDonDto>();
            ChiTietHoaDonListBox.ItemsSource = chiTiet;
            _fullChiTietHoaDonList = chiTiet.ToList();

            TenHoaDonTextBlock.Text =
                $"{hd.Ten}{(string.IsNullOrWhiteSpace(hd.DiaChiText) ? "" : " - " + hd.DiaChiText)}";

            DiemThangNayTextBlock.Text = DiemThangTruocTextBlock.Text = null;

            RightPanelGrid.Background =
                hd.TongNoKhachHang == 0 ? Brushes.DodgerBlue : Brushes.IndianRed;

            RenderFooterPanel(ThongTinThanhToanPanel, hd, includeLine: false);
            UpdateTongSanPham(hd);
        }

        // ══════════════════════════════════════════════════════════════════════
        // THANH TOÁN
        // ══════════════════════════════════════════════════════════════════════

        private async Task ThanhToanAsync(Guid phuongThucId, CancellationToken ct = default)
        {
            var selected = SelectedNoHoaDon;
            if (selected == null) return;
            if (selected.ConLai <= 0) { NotiHelper.Show("Hoá đơn đã thu đủ!"); return; }

            var owner = Window.GetWindow(this);
            var form = new ChiTietHoaDonThanhToanEdit(new ChiTietHoaDonThanhToanDto
            {
                HoaDonId = selected.Id,
                KhachHangId = selected.KhachHangId,
                Ten = selected.TenKhachHangText,
                SoTien = selected.ConLai,
                PhuongThucThanhToanId = phuongThucId
            })
            {
                Owner = owner,
                Width = owner?.ActualWidth ?? 1200,
                Height = owner?.ActualHeight ?? 800
            };

            form.PhuongThucThanhToanComboBox.IsEnabled = false;
            if (form.ShowDialog() != true) return;

            var soTien = form.Model.SoTien;
            if (soTien <= 0) return;
            if (soTien > selected.ConLai) soTien = selected.ConLai;

            var oldConLai = selected.ConLai;
            var oldDaThu = selected.DaThu;
            var oldLastModified = selected.LastModified;
            var lastModified = selected.LastModified;

            // Optimistic update
            selected.DaThu += soTien;
            selected.ConLai = selected.ThanhTien - selected.DaThu;
            selected.RefreshWaitingTime();
            RequestRefresh();

            _ = Task.Run(async () =>
            {
                try
                {
                    ct.ThrowIfCancellationRequested();

                    var r = await _hoaDonApi.UpdateF1F4SingleAsync(selected.Id, new ChiTietHoaDonThanhToanDto
                    {
                        LastModified = lastModified,
                        HoaDonId = selected.Id,
                        KhachHangId = selected.KhachHangId,
                        Ten = selected.TenKhachHangText,
                        SoTien = soTien,
                        PhuongThucThanhToanId = phuongThucId
                    }, ct);

                    if (!r.IsSuccess) NotiHelper.ShowError(r.Message);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (r.Data != null)
                        {
                            UpdateLocalHoaDon(r.Data);
                            HoaDonDataGrid_SelectionChangedAsync(null!, null!);
                        }
                    });
                }
                catch (OperationCanceledException) { }
                catch
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        selected.ConLai = oldConLai;
                        selected.DaThu = oldDaThu;
                        selected.LastModified = oldLastModified;
                        selected.RefreshWaitingTime();
                        RequestRefresh();
                        NotiHelper.ShowError("Thanh toán thất bại!");
                    });
                }
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        // BUTTON HANDLERS
        // ══════════════════════════════════════════════════════════════════════

        private async void F1Button_Click(object sender, RoutedEventArgs e)
            => await ExecuteHoaDonActionAsync(F1Button,
                ct => ThanhToanAsync(AppConstants.TienMatId, ct),
                () => SelectedNoHoaDon != null);

        private async void F4Button_Click(object sender, RoutedEventArgs e)
            => await ExecuteHoaDonActionAsync(F4Button,
                ct => ThanhToanAsync(AppConstants.ChuyenKhoanId, ct),
                () => SelectedNoHoaDon != null);

        private async void F12Button_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(F12Button, async ct =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null) return;

                if (selected.ConLai == 0) { NotiHelper.Show("Hoá đơn đã thu đủ!"); return; }
                if (selected.NgayNo != null) { NotiHelper.Show("Hoá đơn đã ghi nợ rồi!"); return; }
                if (selected.KhachHangId == null) { NotiHelper.Show("Hoá đơn chưa có thông tin khách hàng!"); return; }
                if (selected.PhanLoai == "Ship" && selected.NgayShip == null)
                { NotiHelper.Show("Hoá đơn chưa ESC!"); return; }

                if (MessageBox.Show($"Ghi nợ {selected.ConLai:N0} cho khách hàng?", "Xác nhận ghi nợ",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

                var now = DateTime.Now;
                var ngayNo = selected.NgayGio.HasValue && now.Date > selected.NgayGio.Value.Date
                    ? selected.NgayGio.Value.Date.AddDays(1).AddSeconds(-1) : now;

                var oldNgayNo = selected.NgayNo;
                var lastModified = selected.LastModified;

                selected.NgayNo = ngayNo;
                selected.RefreshWaitingTime();
                RequestRefresh();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        ct.ThrowIfCancellationRequested();

                        var r = await _hoaDonApi.UpdateF12SingleAsync(selected.Id, new HoaDonDto
                        {
                            Id = selected.Id,
                            NgayNo = ngayNo,
                            LastModified = lastModified
                        }, ct);

                        if (!r.IsSuccess) NotiHelper.ShowError(r.Message);
                        await Dispatcher.InvokeAsync(() => UpdateLocalHoaDon(r.Data));
                    }
                    catch (OperationCanceledException) { }
                    catch
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            selected.NgayNo = oldNgayNo;
                            selected.LastModified = lastModified;
                            selected.RefreshWaitingTime();
                            RequestRefresh();
                            NotiHelper.ShowError("Ghi nợ thất bại!");
                        });
                    }
                });

            }, () => SelectedNoHoaDon != null);
        }

        private async void DelButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(DelButton, async ct =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null) return;

                if (MessageBox.Show($"Bạn có chắc chắn muốn xoá '{selected.TenKhachHangText}'?",
                        "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;

                var local = _items.FirstOrDefault(x => x.Id == selected.Id);
                if (local == null) return;

                int index = _items.IndexOf(local);
                _items.Remove(local);
                RequestRefresh();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        ct.ThrowIfCancellationRequested();

                        var response = await ApiClient.DeleteAsync($"/api/HoaDon/{selected.Id}", true, ct);
                        var result = await response.Content.ReadFromJsonAsync<Result<HoaDonDto>>(cancellationToken: ct);

                        if (result?.IsSuccess != true)
                            await Dispatcher.InvokeAsync(() =>
                            {
                                RestoreItem(local, index);
                                NotiHelper.ShowError(result?.Message ?? "Xoá thất bại!");
                            });
                    }
                    catch (OperationCanceledException) { }
                    catch
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            RestoreItem(local, index);
                            NotiHelper.ShowError("Không thể kết nối server!");
                        });
                    }
                });

            }, () => SelectedNoHoaDon != null);
        }

        private async void EscButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(EscButton, async ct =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null) return;

                if (selected.KhachHangId == null)
                { NotiHelper.Show("Hoá đơn chưa có thông tin khách hàng!"); return; }

                var confirm = ShowShipperImageDialog();
                if (confirm == MessageBoxResult.Cancel) return;

                var hoaDon = await GetHoaDonAsync(selected.Id, ct);
                if (hoaDon == null || ct.IsCancellationRequested) return;

                var now = DateTime.Now;
                bool needPrint = hoaDon.NgayIn == null;
                var shipper = confirm == MessageBoxResult.Yes ? "Khánh" : "Nhã";

                var oldNgayShip = selected.NgayShip;
                var oldNguoiShip = selected.NguoiShip;
                var oldNgayIn = selected.NgayIn;
                var oldLastModified = selected.LastModified;
                var lastModified = selected.LastModified;

                selected.NgayShip = now;
                selected.NguoiShip = shipper;
                if (needPrint) selected.NgayIn = now;
                selected.RefreshWaitingTime();
                RequestRefresh();

                if (needPrint) HoaDonPrinter.Print(hoaDon);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        ct.ThrowIfCancellationRequested();

                        var r = await _hoaDonApi.UpdateEscSingleAsync(hoaDon.Id, new HoaDonDto
                        {
                            Id = hoaDon.Id,
                            LastModified = lastModified,
                            NgayShip = now,
                            NguoiShip = shipper,
                            NgayIn = needPrint ? now : hoaDon.NgayIn
                        }, ct);

                        if (!r.IsSuccess) NotiHelper.ShowError(r.Message);
                        await Dispatcher.InvokeAsync(() => UpdateLocalHoaDon(r.Data));
                    }
                    catch (OperationCanceledException) { }
                    catch
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            selected.NgayShip = oldNgayShip;
                            selected.NguoiShip = oldNguoiShip;
                            selected.NgayIn = oldNgayIn;
                            selected.LastModified = oldLastModified;
                            selected.RefreshWaitingTime();
                            RequestRefresh();
                            NotiHelper.ShowError("Gán ship thất bại!");
                        });
                    }
                });

            }, () => SelectedNoHoaDon != null);
        }

        private async void F2Button_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(F2Button, async ct =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null) return;

                var hoaDon = _currentHoaDon;
                if (hoaDon == null || hoaDon.Id != selected.Id) return;

                HoaDonPrinter.Print(hoaDon);

                var now = DateTime.Now;
                var local = _items.FirstOrDefault(x => x.Id == selected.Id);
                var oldNgayIn = local?.NgayIn;
                var lastModified = selected.LastModified;

                if (local != null) { local.NgayIn = now; RequestRefresh(); }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        ct.ThrowIfCancellationRequested();

                        var r = await _hoaDonApi.UpdatePrintSingleAsync(hoaDon.Id, new HoaDonDto
                        {
                            Id = hoaDon.Id,
                            NgayIn = now,
                            LastModified = lastModified
                        }, ct);

                        if (!r.IsSuccess) return;
                        await Dispatcher.InvokeAsync(() => UpdateLocalHoaDon(r.Data));
                    }
                    catch (OperationCanceledException) { }
                    catch
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (local != null) { local.NgayIn = oldNgayIn; RequestRefresh(); }
                        });
                    }
                });

            }, () => SelectedNoHoaDon != null);
        }

        private async void F3Button_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(F3Button, async ct =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null) return;

                var hoaDon = _currentHoaDon;
                if (hoaDon == null || hoaDon.Id != selected.Id) return;

                HoaDonPrinter.PrepareZaloTextAndQr_AlwaysCopy(hoaDon);
                NotiHelper.Show("Đã copy, ctrl+V để gửi!");

            }, () => SelectedNoHoaDon != null);
        }

        private async void F9Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F9Button, async ct =>
            {
                var folder = @"C:\DennMenu";
                if (!Directory.Exists(folder)) { NotiHelper.Show("Thư mục không tồn tại!"); return; }

                var files = Directory.GetFiles(folder)
                    .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (files.Count == 0) { NotiHelper.Show("Không tìm thấy hình!"); return; }

                var sc = new System.Collections.Specialized.StringCollection();
                foreach (var file in files) sc.Add(file);

                Clipboard.SetFileDropList(sc);
                NotiHelper.Show($"Đã copy {sc.Count} hình, Ctrl+V để gửi!");

                await Task.CompletedTask;
            });
        }

        private async void RollBack_Click(object sender, RoutedEventArgs e)
        {
            SelectRow(sender);

            await ExecuteHoaDonActionAsync((sender as Button)!, async ct =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null) return;

                if (MessageBox.Show(
                        $"Huỷ toàn bộ thanh toán và ghi nợ của '{selected.TenKhachHangText}'?",
                        "Rollback hóa đơn", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;

                var r = await _hoaDonApi.UpdateRollBackSingleAsync(
                    selected.Id, new HoaDonDto { LastModified = DateTime.Now }, ct);

                if (!r.IsSuccess) NotiHelper.ShowError(r.Message);

                UpdateLocalHoaDon(r.Data);
                HoaDonDataGrid_SelectionChangedAsync(null!, null!);

            }, () => SelectedNoHoaDon != null);
        }

        private void RowEsc_Click(object sender, RoutedEventArgs e)
        {
            SelectRow(sender);
            EscButton_Click(sender, e);
        }

        // ══════════════════════════════════════════════════════════════════════
        // DOUBLE CLICK / OPEN EDIT
        // ══════════════════════════════════════════════════════════════════════

        private async void HoaDonDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.OverrideCursor == Cursors.Wait) return;

            var selected = SelectedNoHoaDon;
            if (selected == null) return;

            try
            {
                var hoaDon = await GetHoaDonAsync(selected.Id);
                if (hoaDon == null) return;

                var savedId = await OpenHoaDonEditAsync(hoaDon);
                if (!savedId.HasValue) return;

                var updated = await GetHoaDonAsync(savedId.Value);
                if (updated == null) return;

                UpdateOrInsertLocal(updated);
                InvalidateCache(savedId.Value);
                _currentHoaDon = null;

                await SelectHoaDonSafeAsync(savedId.Value);
                HoaDonDataGrid_SelectionChangedAsync(null!, null!);
            }
            catch (Exception ex) { NotiHelper.ShowError(ex.Message); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // ADD BUTTONS
        // ══════════════════════════════════════════════════════════════════════

        private async void AddTaiChoButton_Click(object sender, RoutedEventArgs e) => await OpenHoaDonWithPhanLoai("Tại Chỗ");
        private async void AddMuaVeButton_Click(object sender, RoutedEventArgs e) => await OpenHoaDonWithPhanLoai("Mv");
        private async void AddShipButton_Click(object sender, RoutedEventArgs e) => await OpenHoaDonWithPhanLoai("Ship");
        private async void AddAppButton_Click(object sender, RoutedEventArgs e) => await OpenHoaDonWithPhanLoai("App");
        private async void AddMuaHoButton_Click(object sender, RoutedEventArgs e) => await OpenHoaDonWithPhanLoai("Mh");

        private async void AppButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(AppButton, async ct =>
            {
                await Dispatcher.Yield(DispatcherPriority.Render);

                AppShippingHelperText helper;
                try { helper = await AppShippingHelperFactory.GetAsync(); }
                catch { helper = await AppShippingHelperFactory.CreateAsync("12122431577", "baothanh1991"); }

                var dto = await Task.Run(() => helper.GetFirstOrderPopup(), ct);
                if (dto == null) return;

                var savedId = await OpenHoaDonEditAsync(dto);
                if (savedId.HasValue) await ReloadAndRestoreSelectionAsync(savedId);

            }, null, "Đang lấy đơn App...");
        }

        private async Task OpenHoaDonWithPhanLoai(string phanLoai)
        {
            var dto = new HoaDonDto();
            if (phanLoai != "Mh") dto.PhanLoai = phanLoai;
            else { dto.PhanLoai = "Mv"; dto.VoucherId = AppConstants.VoucherIdMuaHo; }

            var savedId = await OpenHoaDonEditAsync(dto);
            if (!savedId.HasValue) return;

            var updated = await GetHoaDonAsync(savedId.Value);
            if (updated == null) return;

            UpdateOrInsertLocal(updated);
            InvalidateCache(savedId.Value);
            _currentHoaDon = null;

            await SelectHoaDonSafeAsync(savedId.Value);
            HoaDonDataGrid_SelectionChangedAsync(null!, null!);
        }

        private async Task<Guid?> OpenHoaDonEditAsync(HoaDonDto dto)
        {
            var owner = Window.GetWindow(this);
            var window = new HoaDonEdit(dto)
            {
                Owner = owner,
                Width = owner?.ActualWidth ?? 1200,
                Height = owner?.ActualHeight ?? 800,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (window.ShowDialog() != true) return null;

            var savedId = window.SavedHoaDonId ?? window.Model?.Id ?? dto.Id;
            return savedId == Guid.Empty ? null : savedId;
        }

        private async Task SelectHoaDonSafeAsync(Guid id)
        {
            await Dispatcher.InvokeAsync(() => _view?.Refresh(), DispatcherPriority.Background);
            await Dispatcher.InvokeAsync(() =>
            {
                var item = _items.FirstOrDefault(x => x.Id == id);
                if (item == null) return;
                HoaDonDataGrid.SelectedItem = item;
                HoaDonDataGrid.ScrollIntoView(item);
                HoaDonDataGrid.UpdateLayout();
            }, DispatcherPriority.Loaded);
        }

        // ══════════════════════════════════════════════════════════════════════
        // MISC UI
        // ══════════════════════════════════════════════════════════════════════

        private void ChiTietHoaDonListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChiTietHoaDonListBox.SelectedItem is not ChiTietHoaDonDto ct) return;
            var sp = AppProviders.SanPhams.Items.FirstOrDefault(x => x.Ten == ct.TenSanPham);
            ct.DinhLuong = sp?.DinhLuong ?? "";
        }

        private void UpdateTongSanPham(HoaDonDto hd)
        {
            try
            {
                var tongSoSanPham = HoaDonCalculator.TinhTongSoSanPham(
                    hd.ChiTietHoaDons,
                    AppProviders.SanPhams.Items
                );

                TongSoSanPhamTextBlock.Text = tongSoSanPham.ToString("N0");
                TongSoSanPhamTextBlock.Visibility = Visibility.Visible;

                var tong = hd.TongNoKhachHang + hd.ConLai;
                TienThoiButton.Content = tong switch
                {
                    < 100000 => (100000 - tong).ToString("N0"),
                    < 200000 => (200000 - tong).ToString("N0"),
                    < 300000 => (300000 - tong).ToString("N0"),
                    < 400000 => (400000 - tong).ToString("N0"),
                    < 500000 => (500000 - tong).ToString("N0"),
                    _ => "0"
                };
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════════════════
        // HOTKEY
        // ══════════════════════════════════════════════════════════════════════

        public void HandleHotkey(Key key)
        {
            switch (key)
            {
                case Key.Escape: EscButton_Click(this, new RoutedEventArgs()); break;
                case Key.F1: F1Button_Click(this, new RoutedEventArgs()); break;
                case Key.F2: F2Button_Click(this, new RoutedEventArgs()); break;
                case Key.F3: F3Button_Click(this, new RoutedEventArgs()); break;
                case Key.F4: F4Button_Click(this, new RoutedEventArgs()); break;
                case Key.F9: F9Button_Click(this, new RoutedEventArgs()); break;
                case Key.F12: F12Button_Click(this, new RoutedEventArgs()); break;
                case Key.Delete: DelButton_Click(this, new RoutedEventArgs()); break;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // TIMER
        // ══════════════════════════════════════════════════════════════════════

        private DispatcherTimer _timer = null!;

        private void StartWaitingTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _timer.Tick += (_, _) =>
            {
                foreach (var item in _items) item.RefreshWaitingTime();
                // timer refresh dùng RequestRefresh để không spam
                RequestRefresh();
            };
            _timer.Start();
        }

        // ══════════════════════════════════════════════════════════════════════
        // SIGNAL
        // ══════════════════════════════════════════════════════════════════════

        public void HandleHoaDonSignal(Guid id)
        {
            _signalQueue.Enqueue(id);
            _ = ProcessSignalQueue();
        }

        private async Task ProcessSignalQueue()
        {
            if (!await _signalSemaphore.WaitAsync(0)) return;
            try
            {
                while (_signalQueue.TryDequeue(out var id))
                    await UpdateSingleHoaDonAsync(id);
            }
            finally { _signalSemaphore.Release(); }
        }

        private async Task UpdateSingleHoaDonAsync(Guid id)
        {
            try
            {
                var result = await _hoaDonApi.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null) return;

                var incoming = result.Data;

                await Dispatcher.InvokeAsync(() =>
                {
                    var existing = _items.FirstOrDefault(x => x.Id == id);

                    if (existing != null)
                    {
                        if (incoming.LastModified <= existing.LastModified) return;

                        // Invalidate cache vì data mới từ server
                        InvalidateCache(id);
                        UpdateLocalHoaDon(ToNoDto(incoming));
                    }
                    else
                    {
                        _items.Add(ToNoDto(incoming));
                        TTSHelper.DownloadAndPlayGoogleTTSAsync(
                            $"Có đơn mới: {incoming.DiaChiText} {(long)incoming.ThanhTien} đồng");
                        RequestRefresh();
                    }
                });
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════════════════
        // SHIPPER DIALOG
        // ══════════════════════════════════════════════════════════════════════

        private MessageBoxResult ShowShipperImageDialog()
        {
            try
            {
                var win = new ShipperDialog { Owner = Window.GetWindow(this) };
                win.ShowDialog();
                return win.Result;
            }
            catch { return ShowFallbackDialog(); }
        }

        private MessageBoxResult ShowFallbackDialog() =>
            MessageBox.Show(
                "Nếu shipper là Khánh chọn YES\nNếu không phải chọn NO\nHuỷ bỏ chọn CANCEL",
                "QUAN TRỌNG:", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

        public void RefreshVisibleItemsOnly() => HoaDonDataGrid.Items.Refresh();
    }
}

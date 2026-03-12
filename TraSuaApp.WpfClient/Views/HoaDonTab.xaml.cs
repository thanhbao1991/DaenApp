using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TraSuaApp.Shared.Constants;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class HoaDonTab : UserControl
    {
        private readonly DebounceManager _debouncer = new();
        private CancellationTokenSource? _ttsCts;

        private List<HoaDonNoDto> _items = new();
        private List<ChiTietHoaDonDto> _fullChiTietHoaDonList = new();
        private Dictionary<Guid, string>? _bienTheLookup;
        private readonly DashboardApi _api = new();

        public HoaDonTab()
        {
            InitializeComponent();
            Loaded += HoaDonTab_Loaded;
            Unloaded += OnUnloaded;
        }
        private void HoaDonTab_Loaded(object? sender, RoutedEventArgs e)
        {
            StartWaitingTimer();
            ApplyFilter();
            _bienTheLookup = AppProviders.SanPhams.Items
    .SelectMany(sp => sp.BienThe.Select(bt => (bt.Id, sp.TenNhomSanPham)))
    .ToDictionary(x => x.Id, x => x.TenNhomSanPham);
        }
        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            _ttsCts?.Cancel();
            _ttsCts?.Dispose();
            _ttsCts = null;
            _waitingTimer?.Stop();
        }
        private void Debounce(string key, Action action, int delay = 300)
        {
            _debouncer.Debounce(key, delay, action);
        }
        private void StartReadMonNotes(HoaDonDto hd)
        {
            try
            {
                _ttsCts?.Cancel();

                var notes = hd.ChiTietHoaDons?
                    .Where(x => !string.IsNullOrWhiteSpace(x.NoteText))
                    .Select(x => $"Ghi chú: {x.TenSanPham}. {x.NoteText}")
                    .ToList();

                if (notes == null || notes.Count == 0)
                    return;

                _ttsCts = new CancellationTokenSource();
                var token = _ttsCts.Token;

                Task.Run(async () =>
                {
                    await Task.Delay(300, token);

                    foreach (var line in notes)
                    {
                        if (token.IsCancellationRequested)
                            break;

                        TTSHelper.DownloadAndPlayGoogleTTSAsync(line);

                        await Task.Delay(400, token);
                    }

                }, token);
            }
            catch { }
        }
        private void SetRightBusy(bool busy)
        {
            RightBusyOverlay.Visibility = busy
                ? Visibility.Visible
                : Visibility.Collapsed;

            HoaDonDetailPanel.Opacity = 1;
        }
        private async Task SafeButtonHandlerAsync(
            ButtonBase? button,
            Func<ButtonBase?, Task> action,
            Func<bool>? requireSelected = null,
            string? busyText = null)
        {
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
                    await action(button);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError(ex.Message);
            }
        }


        private void ChiTietHoaDonListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChiTietHoaDonListBox.SelectedItem is not ChiTietHoaDonDto ct)
                return;

            var sp = AppProviders.SanPhams.Items
                .FirstOrDefault(x => x.Ten == ct.TenSanPham);

            ct.DinhLuong = sp?.DinhLuong ?? "";
        }
        public void RenderFooterPanel(StackPanel host, HoaDonDto hd, bool includeLine = true)
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

            string VND(decimal v) => $"{v:N0}";

            if (hd.KhachHangId != null)
            {
                var s1 = StarHelper.GetStarText(hd.DiemThangNay);
                var s2 = StarHelper.GetStarText(hd.DiemThangTruoc);
                DiemThangNayTextBlock.Text = s1;
                DiemThangTruocTextBlock.Text = s2;
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
            if (hd.TongDonKhacDangGiao > 0)
            {
                if (includeLine) host.Children.Add(new Separator());
                AddGridRow("Đơn khác:", VND(hd.TongDonKhacDangGiao));
                AddGridRow("TỔNG:", VND(hd.TongNoKhachHang + hd.ConLai + hd.TongDonKhacDangGiao));
            }
        }
        private void ThongTinThanhToanGroupBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;

            double offset = sv.VerticalOffset - Math.Sign(e.Delta) * 48;
            offset = Math.Max(0, Math.Min(offset, sv.ScrollableHeight));

            sv.ScrollToVerticalOffset(offset);
            e.Handled = true;
        }
        private void SearchHoaDonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Debounce("HoaDonSearch", ApplyFilter);
        }


        private readonly HoaDonApi _hoaDonApi = new();
        private HoaDonNoDto? SelectedNoHoaDon => HoaDonDataGrid.SelectedItem as HoaDonNoDto;
        private async Task<HoaDonDto?> GetHoaDonAsync(Guid id)
        {
            var result = await _hoaDonApi.GetByIdAsync(id);

            if (!result.IsSuccess || result.Data == null)
            {
                NotiHelper.ShowError(result.Message);
                return null;
            }

            return result.Data;
        }
        private async Task ReloadAndRestoreSelectionAsync(Guid? preferId = null)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                await Task.WhenAll(
                    AppProviders.ChiTietHoaDonThanhToans.ReloadAsync()
                );


                var response = await _api.GetHoaDon();

                _items = response.IsSuccess
                    ? response.Data ?? new()
                            : new();

                ApplyFilter();

                if (preferId.HasValue)
                {
                    var found = SelectHoaDonById(preferId.Value);

                    if (!found)
                    {
                        HoaDonDataGrid.SelectedItem = null;
                        ScrollToTop();
                    }
                }
                else
                {
                    HoaDonDataGrid.SelectedItem = null;
                    ScrollToTop();
                }
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


        private bool SelectHoaDonById(Guid id)
        {
            var list = HoaDonDataGrid.ItemsSource as IEnumerable<HoaDonNoDto>;
            var item = list?.FirstOrDefault(x => x.Id == id);

            if (item != null)
            {
                HoaDonDataGrid.SelectedItem = item;
                HoaDonDataGrid.ScrollIntoView(item);
                return true;
            }

            return false;
        }
        private async Task ExecuteHoaDonActionAsync(
            Button button,
            Func<Task> action,
            Func<bool>? condition = null)
        {
            await SafeButtonHandlerAsync(
         button,
         _ => action(),
         condition ?? (() => true));
        }
        private async Task ThanhToanAsync(Guid phuongThucId)
        {
            var selected = SelectedNoHoaDon;
            if (selected == null)
                return;

            if (selected.ConLai <= 0)
            {
                NotiHelper.Show("Hoá đơn đã thu đủ!");
                return;
            }

            var now = DateTime.Now;

            DateTime ngay;
            DateTime ngayGio;

            if (selected.NgayNo == null
                && selected.NgayGio.HasValue
                && now.Date > selected.NgayGio.Value.Date)
            {
                // quá ngày hóa đơn + không ghi nợ
                ngayGio = selected.NgayGio.Value.Date.AddDays(1).AddTicks(-1);
                ngay = ngayGio.Date;
            }
            else
            {
                ngayGio = now;
                ngay = now.Date;
            }

            var dto = new ChiTietHoaDonThanhToanDto
            {
                HoaDonId = selected.Id,
                KhachHangId = selected.KhachHangId,
                Ten = selected.TenKhachHangText,
                Ngay = ngay,
                NgayGio = ngayGio,
                SoTien = selected.ConLai,
                PhuongThucThanhToanId = phuongThucId,
                LoaiThanhToan = selected.NgayNo != null ? "Trả nợ" : "Thanh toán"
            };

            var owner = Window.GetWindow(this);

            var form = new ChiTietHoaDonThanhToanEdit(dto)
            {
                Owner = owner,
                Width = owner?.ActualWidth ?? 1200,
                Height = owner?.ActualHeight ?? 800
            };

            form.PhuongThucThanhToanComboBox.IsEnabled = false;

            if (form.ShowDialog() == true)
                await ReloadAndRestoreSelectionAsync(selected.Id);
        }
        private async void F1Button_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(
                F1Button,
                () => ThanhToanAsync(AppConstants.TienMatId),
                () => SelectedNoHoaDon != null);
        }
        private async void F4Button_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(
                F4Button,
                () => ThanhToanAsync(AppConstants.ChuyenKhoanId),
                () => SelectedNoHoaDon != null);
        }
        private async void F12Button_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(F12Button, async () =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null)
                    return;

                if (selected.ConLai == 0)
                {
                    NotiHelper.Show("Hoá đơn đã thu đủ!");
                    return;
                }

                if (selected.NgayNo != null)
                {
                    NotiHelper.Show("Hoá đơn đã ghi nợ rồi!");
                    return;
                }

                if (selected.KhachHangId == null)
                {
                    NotiHelper.Show("Hoá đơn chưa có thông tin khách hàng!");
                    return;
                }

                var confirm = MessageBox.Show(
                    $"Ghi nợ {selected.ConLai:N0} cho khách hàng?",
                    "Xác nhận ghi nợ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                try
                {
                    DateTime now = DateTime.Now;
                    DateTime ngayNo;

                    if (selected.NgayGio.HasValue && now.Date > selected.NgayGio.Value.Date)
                    {
                        // quá ngày hóa đơn → ép về cuối ngày hóa đơn
                        ngayNo = selected.NgayGio.Value.Date.AddDays(1).AddSeconds(-1);
                    }
                    else
                    {
                        // cùng ngày → dùng giờ hiện tại
                        ngayNo = now;
                    }

                    var update = new HoaDonDto
                    {
                        Id = selected.Id,
                        NgayNo = ngayNo
                    };

                    var r = await _hoaDonApi.UpdateSingleAsync(selected.Id, update);

                    if (!r.IsSuccess)
                        throw new Exception(r.Message);

                    await ReloadAndRestoreSelectionAsync();
                }
                catch (Exception ex)
                {
                    NotiHelper.ShowError(ex.Message);
                }

            }, () => SelectedNoHoaDon != null);
        }
        private async void F2Button_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(F2Button, async () =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null)
                    return;

                var hoaDon = await GetHoaDonAsync(selected.Id);
                if (hoaDon != null)
                    HoaDonPrinter.Print(hoaDon);

            }, () => SelectedNoHoaDon != null);
        }
        private async void F3Button_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(F3Button, async () =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null)
                    return;

                var hoaDon = await GetHoaDonAsync(selected.Id);
                if (hoaDon == null)
                    return;

                HoaDonPrinter.PrepareZaloTextAndQr_AlwaysCopy(hoaDon);

                NotiHelper.Show("Đã copy, ctrl+V để gửi!");

            }, () => SelectedNoHoaDon != null);
        }
        private async void DelButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(DelButton, async () =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null)
                    return;

                var confirm = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xoá '{selected.TenKhachHangText}'?",
                    "Xác nhận xoá",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                try
                {
                    var response = await ApiClient.DeleteAsync($"/api/HoaDon/{selected.Id}");
                    var result = await response.Content.ReadFromJsonAsync<Result<HoaDonDto>>();

                    if (result?.IsSuccess != true)
                        throw new Exception(result?.Message ?? "Không thể xoá.");

                    await ReloadAndRestoreSelectionAsync();
                }
                catch (Exception ex)
                {
                    NotiHelper.ShowError($"Xoá thất bại: {ex.Message}");
                }

            }, () => SelectedNoHoaDon != null);
        }
        private async void F9Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F9Button, async _ =>
            {
                var folder = @"C:\DennMenu";

                if (!Directory.Exists(folder))
                {
                    NotiHelper.Show("Thư mục không tồn tại!");
                    return;
                }

                var files = Directory.GetFiles(folder)
                    .Where(f =>
                        f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (files.Count == 0)
                {
                    NotiHelper.Show("Không tìm thấy hình!");
                    return;
                }

                var sc = new System.Collections.Specialized.StringCollection();

                foreach (var file in files)
                    sc.Add(file);

                Clipboard.SetFileDropList(sc);

                NotiHelper.Show($"Đã copy {sc.Count} hình, Ctrl+V để gửi!");

                await Task.CompletedTask;
            });
        }
        private async void EscButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(EscButton, async () =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null)
                    return;

                if (selected.KhachHangId == null)
                {
                    NotiHelper.Show("Hoá đơn chưa có thông tin khách hàng!");
                    return;
                }

                var confirm = ShowShipperImageDialog();
                if (confirm == MessageBoxResult.Cancel)
                    return;

                var hoaDon = await GetHoaDonAsync(selected.Id);
                if (hoaDon == null)
                    return;

                var now = DateTime.Now;

                bool needPrint = hoaDon.NgayRa == null;

                var update = new HoaDonDto
                {
                    Id = hoaDon.Id,
                    NguoiShip = confirm == MessageBoxResult.Yes ? "Khánh" : "Nhã",
                    NgayShip = now,
                    NgayRa = needPrint ? now : hoaDon.NgayRa,
                    LastModified = hoaDon.LastModified
                };

                try
                {
                    var r = await _hoaDonApi.UpdateSingleAsync(hoaDon.Id, update);

                    if (!r.IsSuccess)
                        throw new Exception(r.Message);

                    await ReloadAndRestoreSelectionAsync(selected.Id);

                    if (needPrint)
                        HoaDonPrinter.Print(hoaDon);
                }
                catch (Exception ex)
                {
                    NotiHelper.ShowError($"Lỗi gán ship: {ex.Message}");
                }

            }, () => SelectedNoHoaDon != null);
        }
        private MessageBoxResult ShowShipperImageDialog()
        {
            try
            {

                var win = new ShipperDialog
                {
                    Owner = Window.GetWindow(this)
                };

                win.ShowDialog();

                return win.Result;
            }
            catch
            {
                return ShowFallbackDialog();
            }
        }
        private MessageBoxResult ShowFallbackDialog()
        {
            return MessageBox.Show(
                "Nếu shipper là Khánh chọn YES\nNếu không phải chọn NO\nHuỷ bỏ chọn CANCEL",
                "QUAN TRỌNG:",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
        }


        private void SelectRow(object sender)
        {
            if (sender is FrameworkElement fe && fe.DataContext is HoaDonNoDto row)
                HoaDonDataGrid.SelectedItem = row;
        }
        private async void RollBack_Click(object sender, RoutedEventArgs e)
        {
            SelectRow(sender);

            await ExecuteHoaDonActionAsync((sender as Button), async () =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null) return;

                var confirm = MessageBox.Show(
                    $"Huỷ toàn bộ thanh toán và ghi nợ của '{selected.TenKhachHangText}'?",
                    "Rollback hóa đơn",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                // Xóa thanh toán
                var response = await ApiClient.DeleteAsync($"/api/ChiTietHoaDonThanhToan/byHoaDon/{selected.Id}");

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Không thể xoá thanh toán.");

                // Huỷ ghi nợ
                var update = new HoaDonDto
                {
                    Id = selected.Id,
                    NgayNo = null,
                    NgayShip = null,
                    NguoiShip = null,
                    NgayRa = null,
                    GhiChuShipper = null,
                };

                var r = await _hoaDonApi.UpdateSingleAsync(selected.Id, update);

                if (!r.IsSuccess)
                    throw new Exception(r.Message);

                await ReloadAndRestoreSelectionAsync(selected.Id);


            }, () => SelectedNoHoaDon != null);
        }
        private void RowEsc_Click(object sender, RoutedEventArgs e)
        {
            SelectRow(sender);
            EscButton_Click(sender, e);         // ESC - Gán ship (Khánh)
        }


        private CancellationTokenSource? _selectionCts;
        private async void HoaDonDataGrid_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            CancellationToken token = default;

            try
            {
                TTSHelper.Stop();
                _selectionCts?.Cancel();
                _selectionCts = new CancellationTokenSource();
                token = _selectionCts.Token;

                if (HoaDonDataGrid.SelectedItem is not HoaDonNoDto row)
                {
                    await AnimationHelper.FadeSwitchAsync(HoaDonDetailPanel, null);
                    return;
                }

                SetRightBusy(true);

                await Task.Delay(120, token);

                var result = await _hoaDonApi.GetByIdAsync(row.Id);

                if (token.IsCancellationRequested)
                    return;

                if (HoaDonDataGrid.SelectedItem is not HoaDonNoDto current || current.Id != row.Id)
                    return;

                if (!result.IsSuccess || result.Data == null)
                {
                    NotiHelper.ShowError($"Lỗi: {result.Message}");
                    await AnimationHelper.FadeSwitchAsync(HoaDonDetailPanel, null);
                    return;
                }

                var hd = result.Data;
                var chiTiet = hd.ChiTietHoaDons ?? new ObservableCollection<ChiTietHoaDonDto>();
                ChiTietHoaDonListBox.ItemsSource = chiTiet;
                _fullChiTietHoaDonList = chiTiet.ToList();

                StartReadMonNotes(hd);

                TenHoaDonTextBlock.Text = $"{hd.Ten}{(string.IsNullOrWhiteSpace(hd.DiaChiText) ? "" : " - " + hd.DiaChiText)}";
                DiemThangNayTextBlock.Text = DiemThangTruocTextBlock.Text = null;
                RightPanelGrid.Background = hd.TongNoKhachHang == 0 ? Brushes.DodgerBlue : Brushes.IndianRed;
                RenderFooterPanel(ThongTinThanhToanPanel, hd, includeLine: false);
                UpdateTongSanPham(hd);

                await AnimationHelper.FadeSwitchAsync(
                    HoaDonDetailPanel.Visibility == Visibility.Visible ? HoaDonDetailPanel : null,
                    HoaDonDetailPanel);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError($"Lỗi: {ex.Message}");
            }
            finally
            {
                if (!token.IsCancellationRequested)
                    SetRightBusy(false);
            }
        }
        private async void HoaDonDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.OverrideCursor == Cursors.Wait)
                return;

            var selected = SelectedNoHoaDon;

            if (selected == null)
                return;

            try
            {
                var hoaDon = await GetHoaDonAsync(selected.Id);

                if (hoaDon == null)
                    return;

                var savedId = await OpenHoaDonEditAsync(hoaDon);

                if (!savedId.HasValue)
                    return;

                await ReloadAndRestoreSelectionAsync(savedId);
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError(ex.Message);
            }
        }
        private void UpdateTongSanPham(HoaDonDto hd)
        {
            try
            {
                var excluded = new HashSet<string> { "Thuốc lá", "Ăn vặt", "Nước lon" };

                int sum = hd.ChiTietHoaDons
                    .Where(ct =>
                    {
                        if (!_bienTheLookup.TryGetValue(ct.SanPhamIdBienThe, out var group))
                            return false;

                        return !excluded.Contains(group);
                    })
                    .Sum(ct => ct.SoLuong);

                TongSoSanPhamTextBlock.Text = sum.ToString("N0");
                TongSoSanPhamTextBlock.Visibility = Visibility.Visible;

                var tong = hd.TongNoKhachHang + hd.ConLai;

                if (tong < 100000)
                    TienThoiButton.Content = (100000 - tong).ToString("N0");
                else if (tong < 200000)
                    TienThoiButton.Content = (200000 - tong).ToString("N0");
                else if (tong < 300000)
                    TienThoiButton.Content = (300000 - tong).ToString("N0");
                else if (tong < 400000)
                    TienThoiButton.Content = (400000 - tong).ToString("N0");
                else if (tong < 500000)
                    TienThoiButton.Content = (500000 - tong).ToString("N0");
                else
                    TienThoiButton.Content = "0";
            }
            catch { }
        }
        private void ApplyFilter()
        {
            string keyword = StringHelper.MyNormalizeText(SearchHoaDonTextBox.Text ?? "");

            IEnumerable<HoaDonNoDto> query = _items;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    (x.TimKiem ?? "").ToLower().Contains(keyword));
            }

            var list = query
                .OrderBy(x => x.SortOrder)
                .ThenByDescending(x => x.NgayGio)
                .ToList();

            int stt = 1;
            foreach (var item in list)
                item.Stt = stt++;

            HoaDonDataGrid.ItemsSource = list;

            ThanhTienColumn.Header =
                $"{list.Sum(x => x.ThanhTien):N0}";
        }
        public async void ReloadUI()
        {
            await ReloadAndRestoreSelectionAsync();
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
                case Key.F9: F9Button_Click(this, new RoutedEventArgs()); break;
                case Key.F12: F12Button_Click(this, new RoutedEventArgs()); break;
                case Key.Delete: DelButton_Click(this, new RoutedEventArgs()); break;
            }
        }


        private DispatcherTimer _waitingTimer;
        private void StartWaitingTimer()
        {
            if (_waitingTimer != null)
                return;

            _waitingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };

            _waitingTimer.Tick += (_, __) =>
            {
                var view = System.Windows.Data.CollectionViewSource
                    .GetDefaultView(HoaDonDataGrid.ItemsSource);

                view?.Refresh();
            };

            _waitingTimer.Start();
        }
        private async void AddTaiChoButton_Click(object sender, RoutedEventArgs e)
     => await OpenHoaDonWithPhanLoai("Tại Chỗ");
        private async void AddMuaVeButton_Click(object sender, RoutedEventArgs e)
            => await OpenHoaDonWithPhanLoai("Mv");
        private async void AddShipButton_Click(object sender, RoutedEventArgs e)
            => await OpenHoaDonWithPhanLoai("Ship");
        private async void AddAppButton_Click(object sender, RoutedEventArgs e)
            => await OpenHoaDonWithPhanLoai("App");
        private async void AddMuaHoButton_Click(object sender, RoutedEventArgs e)
            => await OpenHoaDonWithPhanLoai("Mh");
        private async void AppButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(AppButton, async _ =>
            {
                await Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);

                AppShippingHelperText helper;

                try
                {
                    helper = await AppShippingHelperFactory.GetAsync();
                }
                catch
                {
                    helper = await AppShippingHelperFactory.CreateAsync("12122431577", "baothanh1991");
                }

                var dto = await Task.Run(() => helper.GetFirstOrderPopup());

                if (dto == null)
                    return;

                var savedId = await OpenHoaDonEditAsync(dto);

                if (savedId.HasValue)
                    await ReloadAndRestoreSelectionAsync(savedId);

            }, null, "Đang lấy đơn App...");
        }
        private async Task OpenHoaDonWithPhanLoai(string phanLoai)
        {
            var dto = new HoaDonDto();


            if (phanLoai != "Mh")
                dto.PhanLoai = phanLoai;
            else
            {
                dto.PhanLoai = "Mv";
                dto.VoucherId = AppConstants.VoucherIdMuaHo;
            }

            var savedId = await OpenHoaDonEditAsync(dto);

            if (savedId.HasValue)
                await ReloadAndRestoreSelectionAsync(savedId);
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

            var ok = window.ShowDialog() == true;

            if (!ok)
                return null;

            var savedId =
                window.SavedHoaDonId ??
                window.Model?.Id ??
                dto.Id;

            if (savedId == Guid.Empty)
                return null;

            return savedId;
        }
    }
}
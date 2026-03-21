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

        private List<ChiTietHoaDonDto> _fullChiTietHoaDonList = new();
        private Dictionary<Guid, string>? _bienTheLookup;
        private readonly DashboardApi _api = new();
        private ObservableCollection<HoaDonNoDto> _items = new();
        private ICollectionView? _view;

        public HoaDonTab()
        {
            InitializeComponent();
            Loaded += HoaDonTab_Loaded;
            Unloaded += OnUnloaded;
        }
        private void HoaDonTab_Loaded(object? sender, RoutedEventArgs e)
        {
            StartWaitingTimer();

            _view = CollectionViewSource.GetDefaultView(_items);
            HoaDonDataGrid.ItemsSource = _view;

            _view.Filter = FilterHoaDon;

            _view.SortDescriptions.Clear();
            _view.SortDescriptions.Add(
                new SortDescription(nameof(HoaDonNoDto.SortOrder), ListSortDirection.Ascending));
            _view.SortDescriptions.Add(
                new SortDescription(nameof(HoaDonNoDto.NgayGio), ListSortDirection.Descending));

            ApplyFilter();

            if (_items.Count == 0)
            {
                ReloadAndRestoreSelectionAsync();
            }

        }
        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            _ttsCts?.Cancel();
            _ttsCts?.Dispose();
            _ttsCts = null;
            //_waitingTimer?.Stop();
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

                var noteItems = hd.ChiTietHoaDons?
                    .Where(x => !string.IsNullOrWhiteSpace(x.NoteText))
                    .ToList();

                if (noteItems == null || noteItems.Count == 0)
                    return;

                _ttsCts = new CancellationTokenSource();
                var token = _ttsCts.Token;

                Task.Run(async () =>
                {
                    await Task.Delay(300, token);

                    // <= 2 ghi chú → đọc chi tiết
                    if (noteItems.Count <= 3)
                    {
                        foreach (var item in noteItems)
                        {
                            if (token.IsCancellationRequested)
                                break;

                            var line = $"{item.TenSanPham}. {item.NoteText}";
                            await TTSHelper.DownloadAndPlayGoogleTTSAsync(line);

                            await Task.Delay(400, token);
                        }
                    }
                    else
                    {
                        // > 2 ghi chú → đọc tổng hợp
                        var tenKhach = string.IsNullOrWhiteSpace(hd.TenKhachHangText)
                            ? "Khách lẻ"
                            : hd.TenKhachHangText;

                        var tongLy = hd.ChiTietHoaDons?.Sum(x => x.SoLuong) ?? 0;
                        var soLyCoNote = noteItems.Sum(x => x.SoLuong);

                        var summary = $"{tenKhach}, {tongLy} ly,{soLyCoNote} ghi chú";

                        await TTSHelper.DownloadAndPlayGoogleTTSAsync(summary);
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
        public async Task ReloadAndRestoreSelectionAsync(Guid? preferId = null)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // reload các bảng phụ
                await Task.WhenAll(
                    AppProviders.ChiTietHoaDonThanhToans.ReloadAsync()
                );

                var response = await _api.GetHoaDon();

                if (!response.IsSuccess)
                {
                    NotiHelper.ShowError(response.Message);
                    return;
                }

                var newData = response.Data ?? new List<HoaDonNoDto>();

                // ================== PATCH IN-PLACE ==================
                // tạo map để tìm nhanh
                var dict = _items.ToDictionary(x => x.Id);

                // update + add
                foreach (var newItem in newData)
                {
                    if (dict.TryGetValue(newItem.Id, out var oldItem))
                    {
                        // UPDATE FIELD (quan trọng)
                        oldItem.TenKhachHangText = newItem.TenKhachHangText;

                        oldItem.KhachHangId = newItem.KhachHangId;
                        oldItem.VoucherId = newItem.VoucherId;

                        oldItem.ThanhTien = newItem.ThanhTien;
                        oldItem.ConLai = newItem.ConLai;
                        oldItem.DaThu = newItem.DaThu;

                        oldItem.NgayGio = newItem.NgayGio;
                        oldItem.NgayShip = newItem.NgayShip;
                        oldItem.NgayNo = newItem.NgayNo;
                        oldItem.NgayIn = newItem.NgayIn;

                        oldItem.NguoiShip = newItem.NguoiShip;

                        oldItem.GhiChu = newItem.GhiChu;
                        oldItem.GhiChuShipper = newItem.GhiChuShipper;

                        oldItem.IsBank = newItem.IsBank;
                        oldItem.PhanLoai = newItem.PhanLoai;

                        oldItem.Stt = newItem.Stt;

                        oldItem.LastModified = newItem.LastModified;

                        oldItem.RefreshWaitingTime();
                    }
                    else
                    {
                        _items.Add(newItem);
                    }
                }

                // remove item không còn tồn tại
                var newIds = new HashSet<Guid>(newData.Select(x => x.Id));

                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    if (!newIds.Contains(_items[i].Id))
                        _items.RemoveAt(i);
                }

                // ================== REFRESH VIEW ==================
                _view?.Refresh();

                // ================== RESTORE SELECTION ==================
                if (preferId.HasValue)
                {
                    var found = _items.FirstOrDefault(x => x.Id == preferId.Value);

                    if (found != null)
                    {
                        HoaDonDataGrid.SelectedItem = found;
                        HoaDonDataGrid.ScrollIntoView(found);
                    }
                    else
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

            if (selected.NgayGio.HasValue && now.Date > selected.NgayGio.Value.Date)
            {
                ngayGio = selected.NgayGio.Value.Date.AddDays(1).AddSeconds(-1);
                ngay = ngayGio.Date;
            }
            else
            {
                ngayGio = now;
                ngay = now.Date;
            }
            // ==============================
            // 1️⃣ MỞ FORM (INPUT ONLY)
            // ==============================
            var dto = new ChiTietHoaDonThanhToanDto
            {
                HoaDonId = selected.Id,
                KhachHangId = selected.KhachHangId,
                Ten = selected.TenKhachHangText,
                SoTien = selected.ConLai,
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

            // ==============================
            // 2️⃣ LẤY DATA USER NHẬP
            // ==============================
            var soTien = form.Model.SoTien;

            if (soTien <= 0)
                return;

            if (soTien > selected.ConLai)
                soTien = selected.ConLai;

            // ==============================
            // 3️⃣ BACKUP (rollback)
            // ==============================
            var oldConLai = selected.ConLai;
            var oldDaThu = selected.DaThu;
            var oldLastModified = selected.LastModified;

            // ==============================
            // 4️⃣ UPDATE UI NGAY (OPTIMISTIC)
            // ==============================
            selected.DaThu += soTien;
            selected.ConLai = selected.ThanhTien - selected.DaThu;


            selected.RefreshWaitingTime();
            _view?.Refresh();

            // ==============================
            // 5️⃣ CALL API BACKGROUND
            // ==============================
            var lastModified = selected.LastModified;

            _ = Task.Run(async () =>
            {
                try
                {

                    var newDto = new ChiTietHoaDonThanhToanDto
                    {
                        LastModified = lastModified,
                        HoaDonId = selected.Id,
                        KhachHangId = selected.KhachHangId,
                        Ten = selected.TenKhachHangText,
                        TenPhuongThucThanhToan = form.Model.TenPhuongThucThanhToan,
                        SoTien = soTien,
                        PhuongThucThanhToanId = phuongThucId,

                    };

                    var r = await _hoaDonApi.UpdateF1F4SingleAsync(selected.Id, newDto);

                    if (!r.IsSuccess)
                        NotiHelper.ShowError(r.Message);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (r.Data != null)
                        {
                            UpdateLocalHoaDon(r.Data);
                            HoaDonDataGrid_SelectionChangedAsync(null, null);
                        }
                    });
                }
                catch
                {
                    // ==============================
                    // 6️⃣ ROLLBACK
                    // ==============================
                    await Dispatcher.InvokeAsync(() =>
                    {
                        selected.ConLai = oldConLai;
                        selected.DaThu = oldDaThu;
                        selected.LastModified = oldLastModified;

                        selected.RefreshWaitingTime();
                        _view?.Refresh();

                        NotiHelper.ShowError("Thanh toán thất bại!");
                    });
                }
            });
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

                if (selected.PhanLoai == "Ship" && selected.NgayShip == null)
                {
                    NotiHelper.Show("Hoá đơn chưa ESC!");
                    return;
                }

                var confirm = MessageBox.Show(
                    $"Ghi nợ {selected.ConLai:N0} cho khách hàng?",
                    "Xác nhận ghi nợ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                var now = DateTime.Now;

                DateTime ngayNo;
                if (selected.NgayGio.HasValue && now.Date > selected.NgayGio.Value.Date)
                    ngayNo = selected.NgayGio.Value.Date.AddDays(1).AddSeconds(-1);
                else
                    ngayNo = now;

                // ==============================
                // 1️⃣ LƯU STATE CŨ (rollback)
                // ==============================
                var oldNgayNo = selected.NgayNo;
                // ==============================
                // 2️⃣ UPDATE UI NGAY
                // ==============================
                selected.NgayNo = ngayNo;
                selected.RefreshWaitingTime();

                _view?.Refresh();

                // ==============================
                // 3️⃣ SERVER BACKGROUND
                // ==============================
                var lastModified = selected.LastModified;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var update = new HoaDonDto
                        {
                            Id = selected.Id,
                            NgayNo = ngayNo,
                            LastModified = lastModified,
                        };

                        var r = await _hoaDonApi.UpdateF12SingleAsync(selected.Id, update);

                        if (!r.IsSuccess)
                            NotiHelper.ShowError(r.Message);

                        // optional sync lại từ server
                        await Dispatcher.InvokeAsync(() =>
                        {
                            UpdateLocalHoaDon(r.Data);
                        });
                    }
                    catch
                    {
                        // ==============================
                        // 4️⃣ ROLLBACK
                        // ==============================
                        await Dispatcher.InvokeAsync(() =>
                        {
                            selected.NgayNo = oldNgayNo;
                            selected.LastModified = lastModified;
                            selected.RefreshWaitingTime();
                            _view?.Refresh();

                            NotiHelper.ShowError("Ghi nợ thất bại!");
                        });
                    }
                });

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
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                // ==============================
                // 1️⃣ XÓA UI + LƯU LẠI ĐỂ ROLLBACK
                // ==============================
                var local = _items.FirstOrDefault(x => x.Id == selected.Id);
                if (local == null)
                    return;

                int index = _items.IndexOf(local);

                _items.Remove(local);
                _view?.Refresh();

                // ==============================
                // 2️⃣ SERVER DELETE BACKGROUND
                // ==============================

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var response = await ApiClient.DeleteAsync($"/api/HoaDon/{selected.Id}");
                        var result = await response.Content
                            .ReadFromJsonAsync<Result<HoaDonDto>>();

                        if (result?.IsSuccess != true)
                        {
                            // ❗ rollback lại đúng vị trí
                            await Dispatcher.InvokeAsync(() =>
                            {
                                if (!_items.Any(x => x.Id == local.Id))
                                {
                                    if (index >= 0 && index <= _items.Count)
                                        _items.Insert(index, local);
                                    else
                                        _items.Add(local);
                                }

                                _view?.Refresh();
                                NotiHelper.ShowError(result?.Message ?? "Xoá thất bại!");
                            });
                        }
                    }
                    catch
                    {
                        // ❗ rollback nếu lỗi mạng
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (!_items.Any(x => x.Id == local.Id))
                            {
                                if (index >= 0 && index <= _items.Count)
                                    _items.Insert(index, local);
                                else
                                    _items.Add(local);
                            }

                            _view?.Refresh();
                            NotiHelper.ShowError("Không thể kết nối server!");
                        });
                    }
                });

            }, () => SelectedNoHoaDon != null);
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
                bool needPrint = hoaDon.NgayIn == null;

                var shipper = confirm == MessageBoxResult.Yes ? "Khánh" : "Nhã";

                // ==============================
                // 1️⃣ LƯU STATE CŨ (để rollback)
                // ==============================
                var oldNgayShip = selected.NgayShip;
                var oldNguoiShip = selected.NguoiShip;
                var oldNgayIn = selected.NgayIn;
                var oldLastModified = selected.LastModified;

                // ==============================
                // 2️⃣ UPDATE UI NGAY
                // ==============================
                selected.NgayShip = now;
                selected.NguoiShip = shipper;
                if (needPrint)
                    selected.NgayIn = now;

                selected.RefreshWaitingTime();

                _view?.Refresh();

                // In luôn nếu cần
                if (needPrint)
                    HoaDonPrinter.Print(hoaDon);

                // ==============================
                // 3️⃣ CALL SERVER BACKGROUND
                // ==============================
                var lastModified = selected.LastModified;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var update = new HoaDonDto
                        {
                            Id = hoaDon.Id,
                            LastModified = lastModified,
                            NgayShip = now,
                            NguoiShip = shipper,
                            NgayIn = needPrint ? now : hoaDon.NgayIn,
                        };

                        var r = await _hoaDonApi.UpdateEscSingleAsync(hoaDon.Id, update);

                        if (!r.IsSuccess)
                            NotiHelper.ShowError(r.Message);

                        // đồng bộ lại chuẩn từ server (optional)
                        await Dispatcher.InvokeAsync(() =>
                        {
                            UpdateLocalHoaDon(r.Data);
                        });
                    }
                    catch
                    {
                        // ==============================
                        // 4️⃣ ROLLBACK UI
                        // ==============================
                        await Dispatcher.InvokeAsync(() =>
                        {
                            selected.NgayShip = oldNgayShip;
                            selected.NguoiShip = oldNguoiShip;
                            selected.NgayIn = oldNgayIn;
                            selected.LastModified = oldLastModified;

                            selected.RefreshWaitingTime();
                            _view?.Refresh();

                            NotiHelper.ShowError("Gán ship thất bại!");
                        });
                    }
                });

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
        private async void F2Button_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(F2Button, async () =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null)
                    return;

                var hoaDon = _currentHoaDon;

                // đảm bảo đúng item đang chọn
                if (hoaDon == null || hoaDon.Id != selected.Id)
                    return;

                // ==========================
                // 1️⃣ IN NGAY
                // ==========================
                HoaDonPrinter.Print(hoaDon);

                // ==========================
                // 2️⃣ UPDATE NGAYIN (UI trước luôn nếu muốn)
                // ==========================
                var now = DateTime.Now;
                var local = _items.FirstOrDefault(x => x.Id == selected.Id);
                var oldNgayIn = local?.NgayIn;

                if (local != null)
                {
                    local.NgayIn = now;
                    _view?.Refresh();
                }

                // ==========================
                // 3️⃣ SERVER BACKGROUND
                // ==========================
                var lastModified = selected.LastModified;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var update = new HoaDonDto
                        {
                            Id = hoaDon.Id,
                            NgayIn = now,
                            LastModified = lastModified
                        };

                        var r = await _hoaDonApi.UpdatePrintSingleAsync(hoaDon.Id, update);

                        if (!r.IsSuccess)
                            return;

                        // optional sync lại từ server
                        await Dispatcher.InvokeAsync(() =>
                        {
                            UpdateLocalHoaDon(r.Data);
                        });
                    }
                    catch
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (local != null)
                            {
                                local.NgayIn = oldNgayIn;
                                _view?.Refresh();
                            }
                        });
                    }
                });

            }, () => SelectedNoHoaDon != null);
        }
        private async void F3Button_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteHoaDonActionAsync(F3Button, async () =>
            {
                var selected = SelectedNoHoaDon;
                if (selected == null)
                    return;

                var hoaDon = _currentHoaDon;

                // đảm bảo đúng item đang chọn
                if (hoaDon == null || hoaDon.Id != selected.Id)
                    return;

                HoaDonPrinter.PrepareZaloTextAndQr_AlwaysCopy(hoaDon);

                NotiHelper.Show("Đã copy, ctrl+V để gửi!");

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

                var r = await _hoaDonApi.UpdateRollBackSingleAsync(selected.Id, new HoaDonDto { LastModified = DateTime.Now });

                if (!r.IsSuccess)
                    NotiHelper.ShowError(r.Message);

                UpdateLocalHoaDon(r.Data);
                HoaDonDataGrid_SelectionChangedAsync(null, null);

            }, () => SelectedNoHoaDon != null);
        }
        private void RowEsc_Click(object sender, RoutedEventArgs e)
        {
            SelectRow(sender);
            EscButton_Click(sender, e);         // ESC - Gán ship (Khánh)
        }

        private CancellationTokenSource? _selectionCts;
        private HoaDonDto? _currentHoaDon;
        private readonly Dictionary<Guid, HoaDonDto> _hoaDonCache = new();
        private async void HoaDonDataGrid_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            _selectionCts?.Cancel();
            _selectionCts = new CancellationTokenSource();
            var token = _selectionCts.Token;

            HoaDonDto? hd = null;

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

                // ==========================
                // 1️⃣ CACHE FIRST
                // ==========================
                if (_hoaDonCache.TryGetValue(row.Id, out var cached)
                    && cached.LastModified == row.LastModified)
                {
                    hd = cached;
                }

                // ==========================
                // 2️⃣ FETCH IF NEEDED
                // ==========================
                if (hd == null)
                {
                    var result = await _hoaDonApi.GetByIdAsync(row.Id);
                    if (token.IsCancellationRequested) return;

                    if (!result.IsSuccess || result.Data == null)
                    {
                        NotiHelper.ShowError($"Lỗi: {result.Message}");
                        return;
                    }

                    hd = result.Data;
                    _hoaDonCache[row.Id] = hd;
                }

                // ==========================
                // 3️⃣ CHECK RE-SELECT (race safety)
                // ==========================
                if (token.IsCancellationRequested) return;

                if (HoaDonDataGrid.SelectedItem is not HoaDonNoDto current
                    || current.Id != row.Id)
                    return;

                // ==========================
                // 4️⃣ BIND UI
                // ==========================
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

                // ==========================
                // 5️⃣ TTS BACKGROUND
                // ==========================
                _ = Task.Run(() => StartReadMonNotes(hd));

                // ==========================
                // 6️⃣ ANIMATION
                // ==========================
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

                if (savedId.HasValue)
                {
                    var updated = await GetHoaDonAsync(savedId.Value);
                    if (updated != null)
                    {
                        UpdateOrInsertLocal(updated);

                        // ❌ clear cache để tránh dùng data cũ
                        _hoaDonCache.Remove(savedId.Value);
                        _currentHoaDon = null;

                        // ✅ select lại
                        await SelectHoaDonSafeAsync(savedId.Value);

                        // ✅ force reload detail
                        HoaDonDataGrid_SelectionChangedAsync(null, null);
                    }
                }
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
            _view?.Refresh();

            ThanhTienColumn.Header = $"{_items.Sum(x => x.ThanhTien):N0}";
        }
        private bool FilterHoaDon(object obj)
        {
            if (obj is not HoaDonNoDto x)
                return false;

            string keyword = StringHelper.MyNormalizeText(SearchHoaDonTextBox.Text ?? "");

            if (Dashboard.IsThanhToanHidden)
            {
                var oneHourAgo = DateTime.Now.AddHours(-1);

                if (!(x.PhanLoai == "App"
                    || x.NgayGio >= oneHourAgo
                    || (x.NgayNo != null && x.IsBank == true)
                    || x.IsBank == true
                    || x.NgayIn != null
                    || (x.IsBank == false && x.NgayGio?.Second % 59 == 0)))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                if (!(x.TimKiem ?? "").Contains(keyword))
                    return false;
            }

            return true;
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

        private DispatcherTimer _timer;
        private void StartWaitingTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };

            _timer.Tick += (s, e) =>
            {
                foreach (var item in _items)
                {
                    item.RefreshWaitingTime();
                }
            };

            _timer.Start();
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
            {
                var updated = await GetHoaDonAsync(savedId.Value);
                if (updated != null)
                {
                    UpdateOrInsertLocal(updated);

                    // ❌ bỏ cache cũ
                    _hoaDonCache.Remove(savedId.Value);
                    _currentHoaDon = null;

                    // ✅ select lại
                    await SelectHoaDonSafeAsync(savedId.Value);

                    // ✅ force load lại detail
                    HoaDonDataGrid_SelectionChangedAsync(null, null);
                }
            }
        }
        private async Task SelectHoaDonSafeAsync(Guid id)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                _view?.Refresh();
            }, DispatcherPriority.Background);

            await Dispatcher.InvokeAsync(() =>
            {
                var item = _items.FirstOrDefault(x => x.Id == id);
                if (item == null) return;

                HoaDonDataGrid.SelectedItem = item;
                HoaDonDataGrid.ScrollIntoView(item);
                HoaDonDataGrid.UpdateLayout();
            }, DispatcherPriority.Loaded);
        }
        private void UpdateOrInsertLocal(HoaDonDto hd)
        {
            var item = _items.FirstOrDefault(x => x.Id == hd.Id);

            if (item != null)
            {
                // update (reuse hàm cũ)
                UpdateLocalHoaDon(new HoaDonNoDto
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
                    //IsBank = hd.IsBank,
                    PhanLoai = hd.PhanLoai,
                    LastModified = hd.LastModified
                });
            }
            else
            {
                // insert mới
                _items.Add(new HoaDonNoDto
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
                    // IsBank = hd.IsBank,
                    PhanLoai = hd.PhanLoai,
                    LastModified = hd.LastModified
                });
            }

            _view?.Refresh();
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
        private void UpdateLocalHoaDon(HoaDonNoDto dto)
        {
            var item = _items.FirstOrDefault(x => x.Id == dto.Id);
            if (item == null) return;

            item.TenKhachHangText = dto.TenKhachHangText;
            item.KhachHangId = dto.KhachHangId;
            item.VoucherId = dto.VoucherId;

            item.ThanhTien = dto.ThanhTien;
            item.ConLai = dto.ConLai;
            item.DaThu = dto.DaThu;

            item.NgayGio = dto.NgayGio;
            item.NgayShip = dto.NgayShip;
            item.NgayNo = dto.NgayNo;
            item.NgayIn = dto.NgayIn;

            item.NguoiShip = dto.NguoiShip;

            item.GhiChu = dto.GhiChu;
            item.GhiChuShipper = dto.GhiChuShipper;

            item.IsBank = dto.IsBank;
            item.PhanLoai = dto.PhanLoai;

            item.LastModified = dto.LastModified;

            item.RefreshWaitingTime();

            _view?.Refresh();
        }


        //signal
        private readonly ConcurrentQueue<Guid> _signalQueue = new();
        private bool _isProcessingSignal = false;
        public void HandleHoaDonSignal(Guid id)
        {
            _signalQueue.Enqueue(id);

            if (_isProcessingSignal) return;

            _ = ProcessSignalQueue();
        }
        private async Task ProcessSignalQueue()
        {
            _isProcessingSignal = true;

            try
            {
                while (_signalQueue.TryDequeue(out var id))
                {
                    await UpdateSingleHoaDonAsync(id);
                }
            }
            finally
            {
                _isProcessingSignal = false;
            }
        }
        private async Task UpdateSingleHoaDonAsync(Guid id)
        {
            try
            {
                var result = await _hoaDonApi.GetByIdAsync(id);

                if (!result.IsSuccess || result.Data == null)
                    return;

                var incoming = result.Data;

                await Dispatcher.InvokeAsync(() =>
                {
                    var existing = _items.FirstOrDefault(x => x.Id == id);

                    if (existing != null)
                    {
                        if (incoming.LastModified <= existing.LastModified)
                            return;

                        UpdateLocalHoaDon(new HoaDonNoDto
                        {
                            Id = incoming.Id,
                            TenKhachHangText = incoming.TenKhachHangText ?? incoming.TenBan,
                            KhachHangId = incoming.KhachHangId,
                            VoucherId = incoming.VoucherId,
                            ThanhTien = incoming.ThanhTien,
                            ConLai = incoming.ConLai,
                            DaThu = incoming.DaThu,
                            NgayGio = incoming.NgayGio,
                            NgayShip = incoming.NgayShip,
                            NgayNo = incoming.NgayNo,
                            NgayIn = incoming.NgayIn,
                            NguoiShip = incoming.NguoiShip,
                            GhiChu = incoming.GhiChu,
                            GhiChuShipper = incoming.GhiChuShipper,
                            PhanLoai = incoming.PhanLoai,
                            LastModified = incoming.LastModified
                        });
                    }
                    else
                    {
                        _items.Add(new HoaDonNoDto
                        {
                            Id = incoming.Id,
                            TenKhachHangText = incoming.TenKhachHangText ?? incoming.TenBan,
                            KhachHangId = incoming.KhachHangId,
                            VoucherId = incoming.VoucherId,
                            ThanhTien = incoming.ThanhTien,
                            ConLai = incoming.ConLai,
                            DaThu = incoming.DaThu,
                            NgayGio = incoming.NgayGio,
                            NgayShip = incoming.NgayShip,
                            NgayNo = incoming.NgayNo,
                            NgayIn = incoming.NgayIn,
                            NguoiShip = incoming.NguoiShip,
                            GhiChu = incoming.GhiChu,
                            GhiChuShipper = incoming.GhiChuShipper,
                            PhanLoai = incoming.PhanLoai,
                            LastModified = incoming.LastModified
                        });
                        TTSHelper.DownloadAndPlayGoogleTTSAsync(
                        $"Có đơn mới: {incoming.DiaChiText} {((long)incoming.ThanhTien)} đồng");
                    }

                    _view?.Refresh();
                });
            }
            catch { }
        }
        public void RefreshVisibleItemsOnly()
        {
            HoaDonDataGrid.Items.Refresh();
        }
    }
}
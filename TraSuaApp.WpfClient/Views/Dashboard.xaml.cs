using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views
{
    public partial class Dashboard : Window, INotifyPropertyChanged
    {
        // ====== Constants / Keys (tránh magic string) ======
        private const string TAB_TAG_HOADON = "HoaDon";
        private const string TAB_TAG_THONGKE = "ThongKe";
        private const string TAB_TAG_CTHD_NO = "ChiTietHoaDonNo";
        private const string TAB_TAG_CTHD_TT = "ChiTietHoaDonThanhToan";
        private const string TAB_TAG_CHITIEU = "ChiTieuHangNgay";

        private const string PROV_HOADONS = "HoaDons";
        private const string PROV_CONG_VIEC = "CongViecNoiBos";
        private const string PROV_CTHD_NO = "ChiTietHoaDonNos";
        private const string PROV_CTHD_TT = "ChiTietHoaDonThanhToans";
        private const string PROV_CHITIEU = "ChiTieuHangNgays";

        // ====== INotifyPropertyChanged ======
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ====== Freshness cache cho các provider ======
        private readonly Dictionary<string, DateTime> _lastProviderReload = new();
        private readonly TimeSpan _freshnessWindow = TimeSpan.FromSeconds(60);

        // Chặn reload chồng lấn theo từng key
        private readonly Dictionary<string, SemaphoreSlim> _reloadGates = new();

        // Lưu handler để hủy đăng ký khi đóng cửa sổ (tránh memory leak)
        private readonly Dictionary<string, Action> _providerHandlers = new();

        private DateTime _today;
        private DateTime Today
        {
            get => _today;
            set { _today = value; OnPropertyChanged(nameof(Today)); }
        }

        public Dashboard()
        {
            InitializeComponent();

            DataContext = this;
            Loaded += Dashboard_Loaded;

            // Fit tối đa màn hình hiện tại
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

            // Build menus động
            GenerateMenu("Admin", AdminMenu);
            GenerateMenu("HoaDon", HoaDonMenu);
            GenerateMenu("Settings", SettingsMenu);
        }

        // ====== Badge Công việc ======
        private void UpdateCongViecBadge()
        {
            var items = AppProviders.CongViecNoiBos.Items;
            if (items == null) return;

            int count = items.Count(x => !x.DaHoanThanh && !x.IsDeleted);
            Dispatcher.Invoke(() =>
            {
                CvBadgeText.Text = count.ToString();
                CvBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        // ====== Loaded ======
        private async void Dashboard_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Khởi giá trị Today
                Today = DateTime.Today;

                // Badge lần đầu
                UpdateCongViecBadge();

                // Khi danh sách công việc đổi -> cập nhật badge
                AppProviders.CongViecNoiBos.ItemsChanged += (_, __) => UpdateCongViecBadge();

                // Đăng ký sự kiện thay đổi tất cả các provider liên quan Tab
                await BindAllProviders();

                // Load dữ liệu lần đầu
                await AppProviders.ReloadAllAsync();
            }
            catch (Exception ex)
            {
                NotiHelper.Show("Lỗi tải dashboard: " + ex.Message);
            }
        }

        // ====== Busy indicator helper ======
        private async Task WithBusy(Func<Task> body)
        {
            if (ProgressBar != null)
                ProgressBar.Visibility = Visibility.Visible;
            try { await body(); }
            finally
            {
                if (ProgressBar != null)
                    ProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        // ====== Freshness helper (có gate + override thời gian) ======
        private async Task ExecuteWithFreshnessAsync(
            string key,
            Func<Task> reloadAsync,
            Action reloadUi,
            string friendlyNameForToast = "",
            TimeSpan? freshnessOverride = null)
        {
            var window = freshnessOverride ?? _freshnessWindow;

            var now = DateTime.UtcNow;
            bool needReload = !_lastProviderReload.TryGetValue(key, out var last) || (now - last) > window;

            // Gate theo key để tránh reload chồng
            if (!_reloadGates.TryGetValue(key, out var gate))
                _reloadGates[key] = gate = new SemaphoreSlim(1, 1);

            await gate.WaitAsync();
            try
            {
                if (needReload)
                {
                    if (!string.IsNullOrWhiteSpace(friendlyNameForToast))
                        NotiHelper.ShowWarn($"Đang cập nhật {friendlyNameForToast.ToLower()}…");

                    try
                    {
                        await WithBusy(async () =>
                        {
                            await reloadAsync();
                            _lastProviderReload[key] = DateTime.UtcNow;
                        });
                    }
                    catch (Exception ex)
                    {
                        NotiHelper.ShowError($"Lỗi tải {friendlyNameForToast.ToLower()}: {ex.Message}");
                    }
                }
            }
            finally
            {
                gate.Release();
            }

            // Dù có reload hay không, vẫn refresh UI từ cache hiện có (trên UI thread)
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(reloadUi);
            else
                reloadUi();
        }

        // ====== Subscribe tất cả providers 1 lần gọn gàng (đảm bảo UI thread) ======
        private async Task BindAllProviders()
        {
            // Map provider -> handler UI
            void Register(string providerKey, Action subscribeAction, Action onChangedUi)
            {
                // Tạo handler dùng chung
                Action handler = () =>
                {
                    try
                    {
                        if (!Dispatcher.CheckAccess())
                            Dispatcher.Invoke(onChangedUi);
                        else
                            onChangedUi();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"OnChanged '{providerKey}' error: {ex.Message}");
                    }
                };

                // Lưu để hủy về sau
                _providerHandlers[providerKey] = handler;

                // subscribe: đảm bảo -= trước rồi += sau
                subscribeAction.Invoke();
            }

            // HoaDons
            Register(
                PROV_HOADONS,
                subscribeAction: () =>
                {
                    if (AppProviders.HoaDons != null)
                    {
                        if (_providerHandlers.TryGetValue(PROV_HOADONS, out var h)) AppProviders.HoaDons.OnChanged -= h;
                        AppProviders.HoaDons.OnChanged += _providerHandlers[PROV_HOADONS];
                    }
                },
                onChangedUi: () => HoaDonTabControl?.ReloadHoaDonUI()
            );

            // Công việc nội bộ (badge)
            Register(
                PROV_CONG_VIEC,
                subscribeAction: () =>
                {
                    if (AppProviders.CongViecNoiBos != null)
                    {
                        if (_providerHandlers.TryGetValue(PROV_CONG_VIEC, out var h)) AppProviders.CongViecNoiBos.OnChanged -= h;
                        AppProviders.CongViecNoiBos.OnChanged += _providerHandlers[PROV_CONG_VIEC];
                    }
                },
                onChangedUi: UpdateCongViecBadge
            );

            // Chi tiết HĐ nợ
            Register(
                PROV_CTHD_NO,
                subscribeAction: () =>
                {
                    if (AppProviders.ChiTietHoaDonNos != null)
                    {
                        if (_providerHandlers.TryGetValue(PROV_CTHD_NO, out var h)) AppProviders.ChiTietHoaDonNos.OnChanged -= h;
                        AppProviders.ChiTietHoaDonNos.OnChanged += _providerHandlers[PROV_CTHD_NO];
                    }
                },
                onChangedUi: () => ChiTietHoaDonNoTabControl?.ReloadUI()
            );

            // Chi tiết HĐ thanh toán
            Register(
                PROV_CTHD_TT,
                subscribeAction: () =>
                {
                    if (AppProviders.ChiTietHoaDonThanhToans != null)
                    {
                        if (_providerHandlers.TryGetValue(PROV_CTHD_TT, out var h)) AppProviders.ChiTietHoaDonThanhToans.OnChanged -= h;
                        AppProviders.ChiTietHoaDonThanhToans.OnChanged += _providerHandlers[PROV_CTHD_TT];
                    }
                },
                onChangedUi: () =>
                {
                    if (ChiTietHoaDonThanhToanTabControl != null)
                    {
                        ChiTietHoaDonThanhToanTabControl.Today = Today;
                        ChiTietHoaDonThanhToanTabControl.ReloadUI();
                    }
                }
            );

            // Chi tiêu hằng ngày
            Register(
                PROV_CHITIEU,
                subscribeAction: () =>
                {
                    if (AppProviders.ChiTieuHangNgays != null)
                    {
                        if (_providerHandlers.TryGetValue(PROV_CHITIEU, out var h)) AppProviders.ChiTieuHangNgays.OnChanged -= h;
                        AppProviders.ChiTieuHangNgays.OnChanged += _providerHandlers[PROV_CHITIEU];
                    }
                },
                onChangedUi: () =>
                {
                    if (ChiTieuHangNgayTabControl != null)
                    {
                        ChiTieuHangNgayTabControl.Today = Today;
                        ChiTieuHangNgayTabControl.ReloadUI();
                    }
                }
            );

            await Task.CompletedTask;
        }

        // ====== Menu động (Admin/Hóa đơn/Settings) ======
        private void GenerateMenu(string loai, MenuItem m)
        {
            var viewType = typeof(Window);
            var views = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(viewType)
                            && (t.FullName?.Contains(loai) ?? false)
                            && t.Name.Contains("List"))
                .OrderBy(t => t.Name);

            foreach (var view in views)
            {
                string friendly = view.Name.Replace("List", "").Replace("Edit", "");
                string header = (TuDien._tableFriendlyNames != null &&
                                 TuDien._tableFriendlyNames.TryGetValue(friendly, out var lbl) &&
                                 !string.IsNullOrWhiteSpace(lbl))
                                ? lbl
                                : friendly;

                var btn = new MenuItem
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Header = header,
                    Tag = view.Name,
                    ToolTip = loai,
                    Margin = new Thickness(4)
                };
                btn.Click += MenuButton_Click;
                m.Items.Add(btn);
            }
        }

        private async void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem btn || btn.Tag is not string tag) return;

            try
            {
                var namespaceName = $"TraSuaApp.WpfClient.{btn.ToolTip}Views";
                var typeName = $"{namespaceName}.{tag}";
                var type = Type.GetType(typeName);

                if (type == null)
                {
                    NotiHelper.ShowError($"Không tìm thấy form: {tag}");
                    return;
                }

                await Task.Delay(100);

                if (Activator.CreateInstance(type) is Window window)
                {
                    window.Width = Width;
                    window.Height = Height;
                    window.Owner = this;
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError($"Lỗi mở form '{tag}': {ex.Message}");
            }
        }

        // ====== Title bar buttons ======
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
                WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Theo behavior cũ: minimize thay vì close
            MinimizeButton_Click(sender, e);
        }

        // ====== Import Noti Window (giữ nguyên logic) ======
        void Import_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var existing = Application.Current.Windows
                    .OfType<TraSuaApp.WpfClient.Views.NotiWindow>()
                    .FirstOrDefault();

                if (existing == null)
                {
                    var win = new TraSuaApp.WpfClient.Views.NotiWindow();
                    win.Show();
                }
                else
                {
                    existing.Topmost = true;
                    existing.Focus();
                }
            });
        }

        // ====== Chuyển Tab: map rõ ràng + freshness override cho Hoá đơn ======
        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, sender)) return;
            if (sender is not System.Windows.Controls.TabControl) return;

            // Hiệu ứng chuyển
            FrameworkElement? oldContent = (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabItem oldTab)
                ? oldTab.Content as FrameworkElement : null;
            FrameworkElement? newContent = (TabControl.SelectedContent as FrameworkElement);
            await AnimationHelper.FadeSwitchAsync(oldContent, newContent);

            if (TabControl.SelectedItem is not TabItem selectedTab) return;
            string? tag = selectedTab.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            // Thống kê: gọi ReloadToday()
            if (tag == TAB_TAG_THONGKE && selectedTab.Content is ThongKeTab thongKeTab)
            {
                thongKeTab.ReloadToday();
            }

            // Map tag → hành động
            var loadActions = new Dictionary<string, Func<Task>>
            {
                [TAB_TAG_HOADON] = async () =>
                {
                    await ExecuteWithFreshnessAsync(
                        key: PROV_HOADONS,
                        reloadAsync: AppProviders.HoaDons.ReloadAsync,
                        reloadUi: () => HoaDonTabControl?.ReloadHoaDonUI(),
                        friendlyNameForToast: "HĐ",
                        freshnessOverride: TimeSpan.FromSeconds(20) // nhạy hơn cho Hoá đơn
                    );
                },

                [TAB_TAG_CHITIEU] = async () =>
                {
                    await ExecuteWithFreshnessAsync(
                        key: PROV_CHITIEU,
                        reloadAsync: AppProviders.ChiTieuHangNgays.ReloadAsync,
                        reloadUi: () =>
                        {
                            if (ChiTieuHangNgayTabControl != null)
                            {
                                ChiTieuHangNgayTabControl.Today = Today;
                                ChiTieuHangNgayTabControl.ReloadUI();
                            }
                        },
                        friendlyNameForToast: "Chi tiêu hằng ngày");
                },

                [TAB_TAG_CTHD_NO] = async () =>
                {
                    await ExecuteWithFreshnessAsync(
                        key: PROV_CTHD_NO,
                        reloadAsync: AppProviders.ChiTietHoaDonNos.ReloadAsync,
                        reloadUi: () => ChiTietHoaDonNoTabControl?.ReloadUI(),
                        friendlyNameForToast: "Chi tiết HĐ nợ");
                },

                [TAB_TAG_CTHD_TT] = async () =>
                {
                    await ExecuteWithFreshnessAsync(
                        key: PROV_CTHD_TT,
                        reloadAsync: AppProviders.ChiTietHoaDonThanhToans.ReloadAsync,
                        reloadUi: () =>
                        {
                            if (ChiTietHoaDonThanhToanTabControl != null)
                            {
                                ChiTietHoaDonThanhToanTabControl.Today = Today;
                                ChiTietHoaDonThanhToanTabControl.ReloadUI();
                            }
                        },
                        friendlyNameForToast: "Chi tiết HĐ thanh toán");
                },
            };

            if (loadActions.TryGetValue(tag, out var action))
                await action();

            // Nếu là tab Chi tiêu: tự động mở form "Thêm chi tiêu mới"
            if (tag == TAB_TAG_CHITIEU && ChiTieuHangNgayTabControl != null)
            {
                ChiTieuHangNgayTabControl.TriggerAddNew();
            }
        }

        // ====== Hotkeys: forward xuống Tab con ======
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Nếu đang gõ trong TextBox: cho nhập bình thường, chỉ giữ hotkey
            if (Keyboard.FocusedElement is TextBox)
            {
                bool isHotkey =
                    (e.Key == Key.F1 || e.Key == Key.F4 || e.Key == Key.F5) || // hotkey của tab Công nợ
                    (e.Key >= Key.F1 && e.Key <= Key.F24) ||                    // các phím F nói chung
                    e.Key == Key.Escape || e.Key == Key.Delete;                 // nếu bạn cần

                if (!isHotkey) return; // không chặn ký tự gõ
            }

            if (TabControl.SelectedItem is not TabItem tab) return;
            var tag = tab.Tag?.ToString();

            if (tag == TAB_TAG_HOADON && HoaDonTabControl != null)
            {
                HoaDonTabControl.HandleHotkey(e.Key);

                // Chỉ mark handled nếu thật sự là hotkey của tab Hóa đơn
                if ((e.Key >= Key.F1 && e.Key <= Key.F24) || e.Key == Key.Escape || e.Key == Key.Delete || e.Key == Key.Enter || e.Key == Key.Space)
                    e.Handled = true;
            }
            else if (tag == TAB_TAG_CTHD_NO && ChiTietHoaDonNoTabControl != null)
            {
                ChiTietHoaDonNoTabControl.HandleHotkey(e.Key);

                // Chỉ 3 hotkey của tab Công nợ
                if (e.Key == Key.F1 || e.Key == Key.F4 || e.Key == Key.F5)
                    e.Handled = true;
            }
        }

        // ====== Helper tạo DTO trả nợ (giữ nguyên) ======
        private ChiTietHoaDonThanhToanDto TaoDtoTraNo(ChiTietHoaDonNoDto selected, Guid phuongThucId)
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

        // ====== Đóng cửa sổ: dọn dẹp ======
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                // Hủy subscribe sự kiện providers
                if (AppProviders.HoaDons != null && _providerHandlers.TryGetValue(PROV_HOADONS, out var h1))
                    AppProviders.HoaDons.OnChanged -= h1;
                if (AppProviders.CongViecNoiBos != null && _providerHandlers.TryGetValue(PROV_CONG_VIEC, out var h2))
                    AppProviders.CongViecNoiBos.OnChanged -= h2;
                if (AppProviders.ChiTietHoaDonNos != null && _providerHandlers.TryGetValue(PROV_CTHD_NO, out var h3))
                    AppProviders.ChiTietHoaDonNos.OnChanged -= h3;
                if (AppProviders.ChiTietHoaDonThanhToans != null && _providerHandlers.TryGetValue(PROV_CTHD_TT, out var h4))
                    AppProviders.ChiTietHoaDonThanhToans.OnChanged -= h4;
                if (AppProviders.ChiTieuHangNgays != null && _providerHandlers.TryGetValue(PROV_CHITIEU, out var h5))
                    AppProviders.ChiTieuHangNgays.OnChanged -= h5;
            }
            catch { /* ignore */ }

            // Theo code gốc
            AppShippingHelperText.DisposeDriver();
        }

        // ====== Reports Launcher ======
        private readonly Dictionary<string, Func<UserControl>> _reportFactories = new()
        {
            ["UngNha"] = () => new ChiTieuTab(true),
            ["ChiTieu"] = () => new ChiTieuTab(false),
            ["Vouchers"] = () => new VoucherTab(),
            ["SanPham"] = () => new XepHangSanPhamTab(),
            ["KhachHang"] = () => new XepHangKhachHangTab(),
        };

        private void ReportsHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.ContextMenu != null)
            {
                b.ContextMenu.PlacementTarget = b;
                b.ContextMenu.IsOpen = true;
                e.Handled = true; // không đổi selection, chỉ mở menu
            }
        }

        private void ReportMenu_Click(object sender, RoutedEventArgs e)
        {
            TabControl.SelectedIndex = TabControl.Items.Count - 1;
            if (sender is not MenuItem mi) return;

            string key = mi.Tag?.ToString() ?? "";
            string title = mi.Header?.ToString() ?? "Báo cáo";

            if (key == "__Clear")
            {
                ReportsTab.Content = new TextBlock
                {
                    Text = "Chọn báo cáo từ tiêu đề tab...",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontStyle = FontStyles.Italic,
                    Opacity = 0.7
                };
                ReportsHeaderText.Text = "Báo cáo";
                return;
            }

            if (!_reportFactories.TryGetValue(key, out var factory))
            {
                System.Diagnostics.Debug.WriteLine($"No factory for report key: {key}");
                return;
            }

            var content = factory();
            ReportsTab.Content = content;
            ReportsHeaderText.Text = title;
        }
    }
}
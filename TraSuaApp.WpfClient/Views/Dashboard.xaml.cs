using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Json;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FontAwesome.Sharp;
using TraSuaApp.Shared.Config;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Services;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;
using TraSuaApp.WpfClient.Services;
using TraSuaApp.WpfClient.SettingsViews;

namespace TraSuaApp.WpfClient.Views
{
    public class KetQuaDto
    {
        public string? Ten { get; set; } = "";
        public decimal? GiaTri { get; set; }
    }

    public class DebounceDispatcher
    {
        private CancellationTokenSource? _cts;

        public void Debounce(int milliseconds, Action action)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            Task.Delay(milliseconds, _cts.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled) action();
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    // 🟟 Gom debounce vào 1 manager
    public class DebounceManager
    {
        private readonly Dictionary<string, DebounceDispatcher> _map = new();

        public void Debounce(string key, int milliseconds, Action action)
        {
            if (!_map.ContainsKey(key))
                _map[key] = new DebounceDispatcher();

            _map[key].Debounce(milliseconds, action);
        }
    }

    public partial class Dashboard : Window
    {
        public ObservableCollection<KetQuaDto> KetQua { get; set; } = new();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        // 🟟 Timer tách riêng
        private readonly DispatcherTimer _baoDonTimer;
        private readonly DispatcherTimer _congViecTimer;

        // 🟟 Gom debounce
        private readonly DebounceManager _debouncer = new();

        private CancellationTokenSource _cts = new();
        private readonly QuickOrderService _quickOrder;


        public Dashboard()
        {
            InitializeComponent();




            NotiHelper.TargetTextBlock = ThongBaoTextBlock;
            _gpt = new GPTService(Config.apiChatGptKey);
            _quickOrder = new QuickOrderService(Config.apiChatGptKey);
            DataContext = this;
            // 🟟 Timer báo đơn (2s)
            _baoDonTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _baoDonTimer.Tick += async (s, e) => await BaoDonTimer_Tick();
            _baoDonTimer.Start();

            // 🟟 Timer công việc (10 phút)
            _congViecTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(10)
            };
            _congViecTimer.Tick += async (s, e) => await CongViecTimer_Tick();
            _congViecTimer.Start();



            Loaded += Dashboard_Loaded;

            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            GenerateMenu("Admin", AdminMenu);
            GenerateMenu("HoaDon", HoaDonMenu);
            GenerateMenu("Settings", SettingsMenu);
            for (int h = 6; h < 22; h++) GioCombo.Items.Add(h.ToString("D2"));
            for (int m = 0; m < 60; m += 10) PhutCombo.Items.Add(m.ToString("D2"));
        }



        // Chỉ thêm field nếu cần (đặt trong class)
        private readonly int _hoaDonDueBatchSize = 3; // xử lý tối đa 3 HĐ đến hạn mỗi tick

        private async Task BaoDonTimer_Tick()
        {
            // 1) Refresh thời gian hiển thị: CHỈ với danh sách đang hiển thị
            if (DateTime.Now.Second < 5)
            {
                var visible = (HoaDonDataGrid.ItemsSource as IEnumerable<HoaDonDto>)
                              ?? Enumerable.Empty<HoaDonDto>();
                foreach (var item in visible)
                    item.RefreshGioHienThi();
            }

            // 2) HĐ đến hạn: tính off-UI thread + xử lý theo batch nhỏ
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
                    catch { /* log nếu cần */ }
                });
            }
        }
        private readonly int _cvTopN = 5;
        private DateTime _lastCvNotiDate = DateTime.MinValue;
        private readonly HashSet<Guid> _cvNotifiedToday = new(); // tránh lặp TTS trong ngày

        private async Task CongViecTimer_Tick()
        {
            if (AppProviders.CongViecNoiBos == null) return;

            var today = DateTime.Today;
            var list = _fullCongViecNoiBoList
                        .Where(cv => !cv.IsDeleted && !cv.DaHoanThanh);

            // 1) Việc đến NGÀY hẹn = hôm nay
            var dsHenNgay = list
                .Where(cv => cv.NgayCanhBao.HasValue && cv.NgayCanhBao.Value.Date == today)
                .OrderBy(cv => cv.NgayGio ?? DateTime.MaxValue)
                .Take(_cvTopN)
                .ToList();

            // 2) Top-N việc chưa hoàn thành
            var dsChuaHoanThanhTop = list
                .OrderBy(cv => cv.NgayGio ?? DateTime.MaxValue)
                .Take(_cvTopN)
                .ToList();

            // Gom cả hai danh sách lại, tránh trùng Id
            var dsCanDoc = dsHenNgay.Concat(dsChuaHoanThanhTop)
                                    .GroupBy(cv => cv.Id)
                                    .Select(g => g.First())
                                    .ToList();

            // Đọc lần lượt
            foreach (var cv in dsCanDoc)
            {
                if (dsHenNgay.Any(x => x.Id == cv.Id))
                {
                    await TTSHelper.DownloadAndPlayGoogleTTSAsync("Kiểm tra " + cv.Ten.Replace("Nấu", ""));
                }
                else
                {
                    await TTSHelper.DownloadAndPlayGoogleTTSAsync(cv.Ten);
                }
                await Task.Delay(400);
            }
        }


        private DateTime _lastSummaryUpdatedAt = DateTime.MinValue;


        private void SearchHoaDonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                DebounceSearch(tb, "HoaDon", ApplyHoaDonFilter, 300);
        }
        private void SearchChiTietHoaDonNoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                DebounceSearch(tb, "ChiTietNo", ApplyChiTietHoaDonNoFilter);
        }
        private void SearchCongViecNoiBoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                DebounceSearch(tb, "CongViecNoiBo", ApplyCongViecNoiBoFilter);
        }
        private void SearchChiTietHoaDonThanhToanTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                DebounceSearch(tb, "ThanhToan", ApplyChiTietHoaDonThanhToanFilter);
        }
        private void SearchChiTieuHangNgayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                DebounceSearch(tb, "ChiTieu", ApplyChiTieuHangNgayFilter);
        }

        private void ApplyChiTietHoaDonFilter()
        {
            var text = SearchChiTietHoaDonTextBox.Text?.Trim().ToLower() ?? "";

            var sourceList = _fullChiTietHoaDonList
                .Where(x => string.IsNullOrEmpty(text)
                         || x.TenSanPham!.ToLower().Contains(text)
                         || x.TenBienThe!.ToLower().Contains(text)
                         || x.NoteText!.ToLower().Contains(text))
                .ToList();

            // đánh lại STT
            int stt = 1;
            foreach (var item in sourceList)
                item.Stt = stt++;

            ChiTietHoaDonListBox.ItemsSource = sourceList;

            // cập nhật tổng tiền
            decimal tong = sourceList.Sum(x => x.ThanhTien);
            // ChiTietHoaDonTongTextBlock.Text = tong.ToString("N0");

        }
        private void SearchChiTietHoaDonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                DebounceSearch(tb, "ChiTietHoaDon", ApplyChiTietHoaDonFilter);
        }


        private async Task<bool> WaitForDataAsync(Func<bool> condition, int timeoutMs = 5000)
        {
            var sw = Stopwatch.StartNew();
            while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
            {
                await Task.Delay(100);
            }
            return condition();
        }
        private static int x = 0;
        private async void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                today = DateTime.Today.AddDays(x);

                // 🟟 Khởi tạo providers
                await BindAllProviders();

                await AppProviders.ReloadAllAsync();   // 🟟 Gọi reload tất cả
                await UpdateDashboardSummary();
            }
            catch (Exception ex)
            {
                NotiHelper.Show("Lỗi tải dashboard: " + ex.Message);
            }
        }


        private async Task BindProviderAsync(
    Func<bool> waitCondition,
    Action<Action> subscribe,
    Action reloadAction,
    string name)
        {
            await WaitForDataAsync(waitCondition);

            // Ghi chú: đảm bảo debounce capture SynchronizationContext của UI thread
            subscribe(() =>
            {
                Debug.WriteLine($"{DateTime.Now:T} - {name} changed");

                // Đưa về UI thread trước rồi mới debounce để TaskScheduler.FromCurrentSynchronizationContext()
                // trong DebounceDispatcher luôn có context WPF
                Dispatcher.Invoke(() =>
                {
                    _debouncer.Debounce($"prov:{name}", 200, () =>
                    {
                        // Gom nhiều thay đổi trong 150–300ms thành 1 lần render
                        reloadAction();
                        // Gom luôn cập nhật summary vào cùng nhịp
                    });
                });
            });
        }
        private readonly Dictionary<string, DateTime> _lastProviderReload = new();
        private readonly TimeSpan _freshnessWindow = TimeSpan.FromSeconds(60);

        // Helper chạy theo freshness
        private async Task ExecuteWithFreshnessAsync(
            string key,
            Func<Task> reloadAsync,
            Action reloadUi,
            string friendlyNameForToast = "")
        {
            var now = DateTime.UtcNow;
            var needReload = !_lastProviderReload.TryGetValue(key, out var last)
                             || (now - last) > _freshnessWindow;

            if (needReload)
            {
                // Báo nhỏ cho người dùng khi thật sự phải gọi mạng
                if (!string.IsNullOrWhiteSpace(friendlyNameForToast))
                    ThongBaoTextBlock.Text = $"Đang cập nhật {friendlyNameForToast.ToLower()}…";

                try
                {
                    await reloadAsync();
                    _lastProviderReload[key] = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    NotiHelper.ShowError($"Lỗi tải {friendlyNameForToast.ToLower()}: {ex.Message}");
                }
                finally
                {
                    ThongBaoTextBlock.Text = null;
                }
            }

            // Dù reload hay không, vẫn refresh UI từ bộ nhớ hiện có
            reloadUi();
        }
        private async Task BindAllProviders()
        {
            var providers = new List<(Func<bool> wait, Action<Action> subscribe, string name)>
    {
        (() => AppProviders.HoaDons?.Items != null,
            h => { AppProviders.HoaDons!.OnChanged -= h; AppProviders.HoaDons.OnChanged += h; },
            "HoaDons"),

        (() => AppProviders.CongViecNoiBos?.Items != null,
            h => { AppProviders.CongViecNoiBos!.OnChanged -= h; AppProviders.CongViecNoiBos.OnChanged += h; },
            "CongViecNoiBos"),

        (() => AppProviders.ChiTietHoaDonNos?.Items != null,
            h => { AppProviders.ChiTietHoaDonNos!.OnChanged -= h; AppProviders.ChiTietHoaDonNos.OnChanged += h; },
            "ChiTietHoaDonNos"),

        (() => AppProviders.ChiTietHoaDonThanhToans?.Items != null,
            h => { AppProviders.ChiTietHoaDonThanhToans!.OnChanged -= h; AppProviders.ChiTietHoaDonThanhToans.OnChanged += h; },
            "ChiTietHoaDonThanhToans"),

        (() => AppProviders.ChiTieuHangNgays?.Items != null,
            h => { AppProviders.ChiTieuHangNgays!.OnChanged -= h; AppProviders.ChiTieuHangNgays.OnChanged += h; },
            "ChiTieuHangNgays")
    };

            foreach (var (wait, subscribe, name) in providers)
            {
                await BindProviderAsync(wait, subscribe, () => ReloadAllUIIfNeeded(name), name);
            }
        }

        private void ReloadAllUIIfNeeded(string sourceName)
        {
            // Reload tab tương ứng
            switch (sourceName)
            {
                case "HoaDons":
                    ReloadHoaDonUI();
                    break;
                case "CongViecNoiBos":
                    ReloadCongViecNoiBoUI();
                    break;
                case "ChiTietHoaDonNos":
                    ReloadChiTietHoaDonNoUI();
                    break;
                case "ChiTietHoaDonThanhToans":
                    ReloadChiTietHoaDonThanhToanUI();
                    break;
                case "ChiTieuHangNgays":
                    ReloadChiTieuHangNgayUI();
                    break;
            }

            // ✅ Gom reload Thống kê + Dashboard Summary vào chung debounce 1s
            _debouncer.Debounce("ThongKeAndSummary", 1000, async () =>
            {
                Dispatcher.Invoke(() =>
                {
                    var thongKeTab = TabControl.Items
                        .OfType<TabItem>()
                        .FirstOrDefault(t => (t.Tag as string) == "ThongKe")
                        ?.Content as ThongKeTab;

                    thongKeTab?.ReloadToday();
                });

                await UpdateDashboardSummary();
            });
        }

        private List<ChiTietHoaDonDto> _fullChiTietHoaDonList = new();


        private void GenerateMenu(string loai, MenuItem m)
        {
            var viewType = typeof(Window);
            var views = Assembly.GetExecutingAssembly()
      .GetTypes()
      .Where(t => t.IsSubclassOf(viewType)
        && (t.FullName?.Contains(loai) ?? false)
        && t.Name.Contains("List")
      )
      .OrderBy(t => t.Name);

            foreach (var view in views)
            {
                var btn = new MenuItem
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Header =
                    TuDien._tableFriendlyNames[view.Name.Replace("List", "").Replace("Edit", "")],
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
                    window.Width = this.Width;
                    window.Height = this.Height;
                    window.Owner = this;
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError($"Lỗi mở form '{tag}': {ex.Message}");
            }
        }


        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

                this.WindowState = WindowState.Maximized;
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.MinimizeButton_Click(null, null);
            //this.Close();
        }





        private List<CongViecNoiBoDto> _fullCongViecNoiBoList = new();
        private async void AddCongViecNoiBoButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new CongViecNoiBoEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
            };
            if (window.ShowDialog() == true)
                await AppProviders.CongViecNoiBos.ReloadAsync();
        }
        private async void SuaCongViecNoiBoButton_Click(object sender, RoutedEventArgs e)
        {
            if (CongViecNoiBoDataGrid.SelectedItem is not CongViecNoiBoDto selected) return;
            var window = new CongViecNoiBoEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
            };
            if (window.ShowDialog() == true)
                await AppProviders.CongViecNoiBos.ReloadAsync();
        }

        private async void XoaCongViecNoiBoButton_Click(object sender, RoutedEventArgs e)
        {
            if (CongViecNoiBoDataGrid.SelectedItem is not CongViecNoiBoDto selected)
                return;
            var confirm = System.Windows.MessageBox.Show(
               $"Bạn có chắc chắn muốn xoá '{selected.Ten}'?",
               "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/CongViecNoiBo/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<CongViecNoiBoDto>>();

                if (result?.IsSuccess == true)
                {
                    AppProviders.CongViecNoiBos.Remove(selected.Id);
                }
                else
                {
                    _errorHandler.Handle(new Exception(result?.Message ?? "Không thể xoá."), "Delete");
                }
            }
            catch (Exception ex)
            {
                // Khối catch này vẫn hữu ích để bắt các lỗi mạng hoặc lỗi không xác định
                _errorHandler.Handle(ex, "Delete");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

        }
        private async void CongViecNoiBoDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CongViecNoiBoDataGrid.SelectedItem is not CongViecNoiBoDto selected) return;

            selected.DaHoanThanh = !selected.DaHoanThanh;
            selected.NgayGio = DateTime.Now;
            if (selected.DaHoanThanh)
            {
                if (selected.XNgayCanhBao != null && selected.XNgayCanhBao != 0)
                    selected.NgayCanhBao = selected.NgayGio.Value.AddDays(selected.XNgayCanhBao ?? 0);
            }
            else
            {
                selected.NgayCanhBao = null;
            }
            var api = new CongViecNoiBoApi();
            var result = await api.UpdateAsync(selected.Id, selected);

            if (!result.IsSuccess)
            {
                NotiHelper.ShowError($"Lỗi: {result.Message}");
                return;
            }

            var updated = result.Data!;
            selected.DaHoanThanh = updated.DaHoanThanh;
            selected.NgayGio = updated.NgayGio;
            selected.NgayCanhBao = updated.NgayCanhBao;
            selected.LastModified = updated.LastModified;

            SearchCongViecNoiBoTextBox.Text = "";
            ReloadCongViecNoiBoUI();
            SearchCongViecNoiBoTextBox.Focus();
        }
        private async void ReloadCongViecNoiBoUI()
        {
            _fullCongViecNoiBoList = await UiListHelper.BuildListAsync(
                AppProviders.CongViecNoiBos.Items,
                snap => snap.Where(x => !x.IsDeleted)
                            .OrderBy(x => x.DaHoanThanh)
                            .ThenByDescending(x => x.LastModified)
                            .ToList()
            );

            ApplyCongViecNoiBoFilter();
        }
        private void ApplyCongViecNoiBoFilter()
        {
            string keyword = SearchCongViecNoiBoTextBox.Text.Trim().ToLower();
            List<CongViecNoiBoDto> sourceList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                sourceList = _fullCongViecNoiBoList;
            }
            else
            {
                sourceList = _fullCongViecNoiBoList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();
            }

            // Gán số thứ tự
            int stt = 1;
            foreach (var item in sourceList)
            {
                item.Stt = stt++;
            }

            CongViecNoiBoDataGrid.ItemsSource = sourceList;
            // tongTien = sourceList.Sum(x => x.ThanhTien);

            //TongTienCongViecNoiBoTextBlock.Header = $"{tongTien:N0} đ";

        }





        private List<ChiTietHoaDonNoDto> _fullChiTietHoaDonNoList = new();
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

            var window = new ChiTietHoaDonThanhToanEdit(dto)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this
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

            var confirm = System.Windows.MessageBox.Show(
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
        private void ApplyChiTietHoaDonNoFilter()
        {
            string keyword = SearchChiTietHoaDonNoTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
            List<ChiTietHoaDonNoDto> sourceList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                sourceList = _fullChiTietHoaDonNoList;
            }
            else
            {
                sourceList = _fullChiTietHoaDonNoList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();
            }

            // Gán số thứ tự
            int stt = 1;
            foreach (var item in sourceList)
            {
                item.Stt = stt++;
            }

            ChiTietHoaDonNoDataGrid.ItemsSource = sourceList;
            tongTien = sourceList.Sum(x => x.SoTienConLai);

            TongTienChiTietHoaDonNoTextBlock.Header = $"{tongTien:N0} đ";



        }
        private async void ReloadChiTietHoaDonNoUI()
        {
            var todayLocal = today;

            _fullChiTietHoaDonNoList = await UiListHelper.BuildListAsync(
                AppProviders.ChiTietHoaDonNos.Items.Where(x => !x.IsDeleted),
                snap => snap.Where(x => x.SoTienConLai > 0 || x.Ngay == todayLocal)
                            .OrderByDescending(x => x.LastModified)
                            .ToList()
            );

            ApplyChiTietHoaDonNoFilter();
        }





        private List<ChiTietHoaDonThanhToanDto> _fullChiTietHoaDonThanhToanList = new();
        private async void ChiTietHoaDonThanhToanDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChiTietHoaDonThanhToanDataGrid.SelectedItem is not ChiTietHoaDonThanhToanDto selected) return;

            var window = new ChiTietHoaDonThanhToanEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this
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
        private void ApplyChiTietHoaDonThanhToanFilter()
        {

            string keyword = SearchChiTietHoaDonThanhToanTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
            List<ChiTietHoaDonThanhToanDto> sourceList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                sourceList = _fullChiTietHoaDonThanhToanList;
            }
            else
            {
                // Tách keyword theo khoảng trắng
                keyword = TextSearchHelper.NormalizeText(keyword);
                var keywords = keyword
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(k => k.ToLower())
                    .ToList();

                sourceList = _fullChiTietHoaDonThanhToanList
                    .Where(x =>
                    {
                        var text = x.TimKiem.ToLower();
                        // phải chứa tất cả các từ khóa
                        return keywords.All(k => text.Contains(k));
                    })
                    .ToList();
            }



            // Gán số thứ tự
            int stt = 1;
            foreach (var item in sourceList)
            {
                item.Stt = stt++;
            }

            ChiTietHoaDonThanhToanDataGrid.ItemsSource = sourceList;
            tongTien = sourceList.Sum(x => x.SoTien);

            TongTienThanhToanTextBlock.Header = $"{tongTien:N0} đ";
        }
        private async void ReloadChiTietHoaDonThanhToanUI()
        {
            var todayLocal = today;

            _fullChiTietHoaDonThanhToanList = await UiListHelper.BuildListAsync(
                AppProviders.ChiTietHoaDonThanhToans.Items.Where(x => !x.IsDeleted),
                snap => snap.Where(x => x.Ngay == todayLocal)
                            .OrderByDescending(x => x.NgayGio)
                            .ToList()
            );

            ApplyChiTietHoaDonThanhToanFilter();
        }





        private List<ChiTieuHangNgayDto> _fullChiTieuHangNgayList = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private async void AddChiTieuHangNgayButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ChiTieuHangNgayEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
            };

            if (window.ShowDialog() == true)
            {
                await ReloadAfterHoaDonChangeAsync(reloadChiTieu: true);
                AddChiTieuHangNgayButton_Click(null, null);
            }
        }
        private async void ChiTieuHangNgayDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChiTieuHangNgayDataGrid.SelectedItem is not ChiTieuHangNgayDto selected) return;

            var window = new ChiTieuHangNgayEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                await ReloadAfterHoaDonChangeAsync(reloadChiTieu: true);
            }
        }
        private async void XoaChiTieuHangNgayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTieuHangNgayDataGrid.SelectedItem is not ChiTieuHangNgayDto selected)
                return;

            var confirm = MessageBox.Show(
              $"Bạn có chắc chắn muốn xoá '{selected.Ten}'?",
              "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/ChiTieuHangNgay/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<ChiTieuHangNgayDto>>();

                if (result?.IsSuccess == true)
                {
                    await ReloadAfterHoaDonChangeAsync(reloadChiTieu: true);
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
        private void ApplyChiTieuHangNgayFilter()
        {
            string keyword = SearchChiTieuHangNgayTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
            List<ChiTieuHangNgayDto> sourceList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                sourceList = _fullChiTieuHangNgayList;
            }
            else
            {
                sourceList = _fullChiTieuHangNgayList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();
            }

            // Gán số thứ tự
            int stt = 1;
            foreach (var item in sourceList)
            {
                item.Stt = stt++;
            }

            ChiTieuHangNgayDataGrid.ItemsSource = sourceList;
            tongTien = sourceList.Sum(x => x.ThanhTien);

            TongTienChiTieuHangNgayTextBlock.Header = $"{tongTien:N0} đ";

        }
        private async void ReloadChiTieuHangNgayUI()
        {
            var todayLocal = today;

            _fullChiTieuHangNgayList = await UiListHelper.BuildListAsync(
                AppProviders.ChiTieuHangNgays.Items.Where(x => !x.IsDeleted),
                snap => snap.Where(x => x.Ngay == todayLocal && !x.BillThang)
                            // .OrderBy(x => x.BillThang) // nếu cần dùng lại
                            .OrderByDescending(x => x.NgayGio)
                            .ToList()
            );

            ApplyChiTieuHangNgayFilter();
        }




        private async Task<(decimal Total, List<(string Label, decimal Value)> Items)> GetDoanhThuData()
        {
            await WaitForDataAsync(() => AppProviders.HoaDons?.Items != null);

            var groups = AppProviders.HoaDons.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .GroupBy(x => x.PhanLoai)
                .Select(g => (Label: g.Key ?? "Khác", Value: g.Sum(x => x.ThanhTien)))
                .OrderByDescending(g => g.Value)
                .ToList();

            return (groups.Sum(x => x.Value), groups);
        }

        // 🟟 Đã thu
        private async Task<(decimal Total, List<(string Label, decimal Value)> Items)> GetDaThuData()
        {
            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonThanhToans?.Items != null);

            var groups = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => x.LoaiThanhToan.ToLower().Contains("trong ngày"))
                .GroupBy(x => x.TenPhuongThucThanhToan)
                .Select(g => (Label: g.Key ?? "Khác", Value: g.Sum(x => x.SoTien)))
                .OrderByDescending(g => g.Value)
                .ToList();

            return (groups.Sum(x => x.Value), groups);
        }

        // 🟟 Chưa thu
        private async Task<(decimal Total, List<(string Label, decimal Value)> Items)> GetChuaThuData()
        {
            await WaitForDataAsync(() => AppProviders.HoaDons?.Items != null);

            var items = AppProviders.HoaDons.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => x.ConLai > 0 && (x.TrangThai == "Chưa thu" || x.TrangThai == "Thu một phần"))
                .OrderByDescending(x => x.NgayGio)
                .Select(hd => (Label: hd.KhachHangId != null ? hd.TenKhachHangText : hd.TenBan, Value: hd.ConLai))
                .ToList();

            return (items.Sum(x => x.Value), items);
        }

        // 🟟 Chi tiêu
        private async Task<(decimal Total, List<(string Label, decimal Value)> Items)> GetChiTieuData()
        {
            await WaitForDataAsync(() => AppProviders.ChiTieuHangNgays?.Items != null);

            var groups = AppProviders.ChiTieuHangNgays.Items
                .Where(x => !x.IsDeleted && x.Ngay == today && !x.BillThang)
                .GroupBy(x => x.Ten)
                .Select(g => (Label: g.Key ?? "Khác", Value: g.Sum(x => x.ThanhTien)))
                .OrderByDescending(g => g.Value)
                .ToList();

            return (groups.Sum(x => x.Value), groups);
        }

        // 🟟 Công nợ
        private async Task<(decimal Total, List<(string Label, decimal Value)> Items)> GetCongNoData()
        {
            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonNos?.Items != null);

            var groups = AppProviders.ChiTietHoaDonNos.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .GroupBy(x => x.Ten)
                .Select(g => (Label: g.Key ?? "Khách lạ", Value: g.Sum(x => x.SoTienConLai)))
                .OrderByDescending(g => g.Value)
                .ToList();

            return (groups.Sum(x => x.Value), groups);
        }

        // 🟟 Trả nợ bank
        private async Task<(decimal Total, List<(string Label, decimal Value)> Items)> GetTraNoBankData()
        {
            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonThanhToans?.Items != null);

            var groups = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => x.LoaiThanhToan.ToLower() == "trả nợ qua ngày")
                .Where(x => x.TenPhuongThucThanhToan?.ToLower() != "tiền mặt")
                .GroupBy(x => x.KhachHangId)
                .Select(g => (Label: g.FirstOrDefault()?.Ten ?? "Khách lạ", Value: g.Sum(x => x.SoTien)))
                .OrderByDescending(g => g.Value)
                .ToList();

            return (groups.Sum(x => x.Value), groups);
        }

        // 🟟 Trả nợ tiền mặt
        private async Task<(decimal Total, List<(string Label, decimal Value)> Items)> GetTraNoTienData()
        {
            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonThanhToans?.Items != null);

            var groups = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => x.LoaiThanhToan == "Trả nợ qua ngày")
                .Where(x => x.TenPhuongThucThanhToan == "Tiền mặt")
                .GroupBy(x => x.KhachHangId)
                .Select(g => (Label: g.FirstOrDefault()?.Ten ?? "Khách lạ", Value: g.Sum(x => x.SoTien)))
                .OrderByDescending(g => g.Value)
                .ToList();

            return (groups.Sum(x => x.Value), groups);
        }
        private async Task UpdateDashboardSummary()
        {
            try
            {
                var doanhThu = await GetDoanhThuData();
                var daThu = await GetDaThuData();
                var chuaThu = await GetChuaThuData();
                var traNoTien = await GetTraNoTienData();
                var traNoBank = await GetTraNoBankData();
                var chiTieu = await GetChiTieuData();
                var congNo = await GetCongNoData();





                _lastSummaryUpdatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UpdateDashboardSummary lỗi: " + ex.Message);
            }
        }


        // 🟟 Helper auto-grow cho TextBox đa dòng
        private static void AdjustAutoHeightIfMultiline(TextBox tb)
        {
            if (tb == null || tb.AcceptsReturn == false) return;

            int lc = Math.Max(1, tb.LineCount);
            double newHeight = 32 * lc; // mỗi dòng cao 32px
            if (Math.Abs(tb.Height - newHeight) > 0.1)
                tb.Height = newHeight;
        }

        // 🟟 Helper debounce chuẩn cho mọi ô search
        private void DebounceSearch(TextBox tb, string key, Action applyFilter, int delayMs = 300)
        {
            _debouncer.Debounce(key, delayMs, () =>
            {
                AdjustAutoHeightIfMultiline(tb); // chỉ auto-grow nếu multiline
                applyFilter();                   // sau đó mới lọc UI
            });
        }



        private List<HoaDonDto> _fullHoaDonList = new();
        private async void OpenHoaDonWithPhanLoai(string phanLoai)
        {
            var dto = new HoaDonDto
            {
                PhanLoai = phanLoai
            };


            var window = new HoaDonEdit(dto)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
            };

            if (window.ShowDialog() == true)
            {
                // Đảm bảo nhìn thấy đơn mới ngay
                today = DateTime.Today;                     // ép scope về Hôm nay
                await ReloadAfterHoaDonChangeAsync(
                    reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
                SearchHoaDonTextBox.Clear();

            }
        }

        private void AddTaiChoButton_Click(object sender, RoutedEventArgs e)
            => OpenHoaDonWithPhanLoai("Tại Chỗ");

        private void AddMuaVeButton_Click(object sender, RoutedEventArgs e)
            => OpenHoaDonWithPhanLoai("Mv");

        private void AddShipButton_Click(object sender, RoutedEventArgs e)
            => OpenHoaDonWithPhanLoai("Ship");

        private void AddAppButton_Click(object sender, RoutedEventArgs e)
            => OpenHoaDonWithPhanLoai("App");





        private async void HoaDonDataGrid_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Ngắt phát giọng đọc cũ
                _cts?.Cancel();
                TTSHelper.Stop();

                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                if (HoaDonDataGrid.SelectedItem is HoaDonDto selected)
                {
                    HoaDonDetailPanel.DataContext = selected;
                    await AnimationHelper.FadeSwitchAsync(HoaDonDetailPanel.Visibility == Visibility.Visible ? HoaDonDetailPanel : null,
                                                           HoaDonDetailPanel);
                    //ShowHoaDonDetail();

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

                    // Tắt báo đơn ngay
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

                            // Giữ lại selection cũ sau reload
                            var items = HoaDonDataGrid.ItemsSource as System.Collections.Generic.IEnumerable<HoaDonDto>;
                            var again = items?.FirstOrDefault(x => x.Id == selected.Id);
                            if (again != null) HoaDonDataGrid.SelectedItem = again;
                        }
                    }

                    // Cập nhật UI
                    ChiTietHoaDonListBox.ItemsSource = hd.ChiTietHoaDons;
                    UpdateThongTinThanhToanStyle(hd);
                    ThongTinThanhToanPanel.DataContext = hd;

                    RenderFooterPanel(ThongTinThanhToanPanel, hd, includeLine: false);
                    TenHoaDonTextBlock.Text = $"{hd.Ten} - {hd.DiaChiText}";

                    TongSoSanPhamTextBlock.Text = hd.ChiTietHoaDons
                        .Where(ct =>
                        {
                            var bienThe = AppProviders.SanPhams.Items
                                .SelectMany(sp => sp.BienThe)
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

                    // Đọc ghi chú bằng TTS
                    if (selected.PhanLoai != "Tại Chỗ")
                    {
                        foreach (var ct in hd.ChiTietHoaDons)
                        {
                            if (token.IsCancellationRequested) return;

                            if (!string.IsNullOrEmpty(ct.NoteText))
                            {
                                string soLuongChu = NumberToVietnamese(ct.SoLuong);
                                string text = $"{soLuongChu} Ly {ct.TenSanPham}";

                                await TTSHelper.DownloadAndPlayGoogleTTSAsync(text);
                                await Task.Delay(300, token);

                                await TTSHelper.DownloadAndPlayGoogleTTSAsync(ct.NoteText.Replace("#", ""));
                                await Task.Delay(1000, token);
                            }
                        }
                    }
                }
                else
                {
                    await AnimationHelper.FadeSwitchAsync(HoaDonDetailPanel, null);
                    // HideHoaDonDetail();
                }
            }
            catch (OperationCanceledException)
            {
                // Bị hủy -> bỏ qua
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError($"Lỗi: {ex.Message}");
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
                var spacer = new TextBlock { Text = " ", FontSize = 18 }; // chiếm Star để đẩy cột tiền sang phải
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
                // if (hd.TongNoKhachHang != hd.ConLai)
                AddGridRow("TỔNG:", VND(hd.TongNoKhachHang + hd.ConLai));
            }

            if (includeLine) host.Children.Add(new Separator());
        }
        private void UpdateThongTinThanhToanStyle(HoaDonDto hd)
        {
            // mặc định
            ThongTinThanhToanGroupBox.Background = (Brush)System.Windows.Application.Current.Resources["LightBrush"];
            ThongTinThanhToanGroupBox.Foreground = (Brush)System.Windows.Application.Current.Resources["DarkBrush"];

            // Ưu tiên: nếu còn nợ khách hàng > 0 thì luôn hiển thị đỏ nhạt
            if (hd.TongNoKhachHang > 0)
            {
                ThongTinThanhToanGroupBox.Background = (Brush)System.Windows.Application.Current.Resources["DangerBrush"];
                ThongTinThanhToanGroupBox.Foreground = (Brush)System.Windows.Application.Current.Resources["LightBrush"];
                return; // dừng ở đây, không xét tiếp
            }

            // Nếu không có công nợ, xét tiếp theo trạng thái
            switch (hd.TrangThai)
            {
                case "Tiền mặt":
                    ThongTinThanhToanGroupBox.Background = (Brush)System.Windows.Application.Current.Resources["SuccessBrush"];
                    ThongTinThanhToanGroupBox.Foreground = (Brush)System.Windows.Application.Current.Resources["LightBrush"];
                    break;

                case "Chuyển khoản":
                    ThongTinThanhToanGroupBox.Background = (Brush)System.Windows.Application.Current.Resources["WarningBrush"];
                    ThongTinThanhToanGroupBox.Foreground = (Brush)System.Windows.Application.Current.Resources["DarkBrush"];
                    break;

                case "Banking Nhã":
                    ThongTinThanhToanGroupBox.Background = (Brush)System.Windows.Application.Current.Resources["WarningBrush"];
                    ThongTinThanhToanGroupBox.Foreground = (Brush)System.Windows.Application.Current.Resources["DarkBrush"];
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
                    ThongTinThanhToanGroupBox.Foreground = (Brush)System.Windows.Application.Current.Resources["DarkBrush"];
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
                    ThongTinThanhToanGroupBox.Foreground = (Brush)System.Windows.Application.Current.Resources["DarkBrush"];
                    break;

                case "Thu một phần":
                    ThongTinThanhToanGroupBox.Background = (Brush)System.Windows.Application.Current.Resources["SuccessBrush"];
                    ThongTinThanhToanGroupBox.Foreground = (Brush)System.Windows.Application.Current.Resources["DarkBrush"];
                    break;

                case "Nợ một phần":
                    ThongTinThanhToanGroupBox.Background = (Brush)System.Windows.Application.Current.Resources["DangerBrush"];
                    ThongTinThanhToanGroupBox.Foreground = (Brush)System.Windows.Application.Current.Resources["DarkBrush"]; // đỏ nhạt → chữ đen vẫn đọc được
                    break;

                case "Ghi nợ":
                    ThongTinThanhToanGroupBox.Background = (Brush)System.Windows.Application.Current.Resources["DangerBrush"];
                    ThongTinThanhToanGroupBox.Foreground = (Brush)System.Windows.Application.Current.Resources["LightBrush"];
                    break;
            }
        }
        private string NumberToVietnamese(int number)
        {
            string[] nums = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };

            if (number < 10) return nums[number];

            if (number < 20)
            {
                if (number == 10) return "mười";
                if (number == 15) return "mười lăm";
                return "mười " + nums[number % 10];
            }

            if (number < 100)
            {
                int tens = number / 10;
                int ones = number % 10;
                string result = nums[tens] + " mươi";
                if (ones == 1) result += " mốt";
                else if (ones == 5) result += " lăm";
                else if (ones > 0) result += " " + nums[ones];
                return result;
            }

            return number.ToString();
        }
        // Thêm vào lớp hỗ trợ của bạn (có thể trong Dashboard.xaml.cs)
        private async void HoaDonDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;
            try
            {
                // 🟟 Lấy lại hóa đơn đầy đủ từ DB (bao gồm ChiTietHoaDons, Topping, Voucher...)
                var api = new HoaDonApi();
                var result = await api.GetByIdAsync(selected.Id);

                if (!result.IsSuccess || result.Data == null)
                {
                    NotiHelper.ShowError($"Không thể tải chi tiết hóa đơn: {result.Message}");
                    return;
                }

                var window = new HoaDonEdit(result.Data)
                {
                    Width = this.ActualWidth,
                    Height = this.ActualHeight,
                    Owner = this
                };
                if (window.ShowDialog() == true)
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
            }
            finally
            {
            }


        }
        private void ApplyHoaDonFilter()
        {
            string keyword = SearchHoaDonTextBox.Text.Trim().ToLower();
            List<HoaDonDto> sourceList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                sourceList = _fullHoaDonList;
            }
            else
            {
                sourceList = _fullHoaDonList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();
            }

            // 🟟 Không sort ở đây nữa, chỉ gán STT
            int stt = 1;
            foreach (var item in sourceList)
            {
                item.Stt = stt++;
            }

            HoaDonDataGrid.ItemsSource = sourceList;
            //TongTienHoaDonTextBlock.Header = $"{sourceList.Sum(x => x.ThanhTien):N0} đ";
        }
        private async void ReloadHoaDonUI()
        {
            _fullHoaDonList = await UiListHelper.BuildListAsync(
            AppProviders.HoaDons.Items.Where(x => !x.IsDeleted),
            snap => snap.Where(x => x.Ngay.Date == today.Date || x.DaThuHoacGhiNo)
                .OrderBy(x =>
                {
                    // Ưu tiên nhóm
                    if (x.UuTien) return 0; // Nhóm 1: Đơn ưu tiên
                    if (x.PhanLoai == "Ship" && x.NgayShip == null) return 1; // Nhóm 2: Ship chưa đi
                    if (x.PhanLoai != "Ship" &&
                        (x.TrangThai == "Chưa thu" || x.TrangThai == "Thu một phần" || x.TrangThai == "Chuyển khoản một phần"))
                        return 2; // Nhóm 3: Đơn khác ship chưa thu
                    if (x.PhanLoai == "Ship" && x.NgayShip != null && !x.DaThuHoacGhiNo) return 3; // Nhóm 4 (mới): Ship đã đi nhưng chưa thu
                    return 4; // Nhóm 5: Các đơn còn lại
                })
                .ThenByDescending(x => x.NgayGio) // trong nhóm thì sắp theo thời gian mới nhất
        );


            ApplyHoaDonFilter();
        }

        private void ChiTietHoaDonListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChiTietHoaDonListBox.SelectedItem is not ChiTietHoaDonDto selected)
            {
                ThongBaoTextBlock.Text = "";
                return;
            }
            var sp = AppProviders.SanPhams.Items.SingleOrDefault(x => x.Ten == selected.TenSanPham);
            selected.DinhLuong = sp == null ? "" : sp.DinhLuong;
        }


        private DateTime today;

        private readonly GPTService _gpt;

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (!ReferenceEquals(e.OriginalSource, sender))
                return;

            if (sender is not TabControl tabControl) return;

            FrameworkElement? oldContent = (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabItem oldTab)
                                           ? oldTab.Content as FrameworkElement
                                           : null;

            FrameworkElement? newContent = tabControl.SelectedContent as FrameworkElement;

            await AnimationHelper.FadeSwitchAsync(oldContent, newContent);

            var selectedTab = TabControl.SelectedItem as TabItem;
            if (selectedTab == null) return;

            var tag = selectedTab.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            if (tag == "ThongKe")
            {
                if (selectedTab.Content is ThongKeTab thongKeTab)
                {
                    thongKeTab.ReloadToday();
                }
            }


            ThongBaoTextBlock.Text = null;

            // Map tag → action load lại dữ liệu
            var loadActions = new Dictionary<string, Func<Task>>
            {
                ["HoaDon"] = async () =>
                {
                    await ExecuteWithFreshnessAsync(
                        key: "HoaDons",
                        reloadAsync: AppProviders.HoaDons.ReloadAsync,
                        reloadUi: ReloadHoaDonUI,
                        friendlyNameForToast: "Hóa đơn");
                },

                ["ChiTieuHangNgay"] = async () =>
                {
                    await ExecuteWithFreshnessAsync(
                        key: "ChiTieuHangNgays",
                        reloadAsync: AppProviders.ChiTieuHangNgays.ReloadAsync,
                        reloadUi: ReloadChiTieuHangNgayUI,
                        friendlyNameForToast: "Chi tiêu hằng ngày");
                },

                ["ChiTietHoaDonNo"] = async () =>
                {
                    await ExecuteWithFreshnessAsync(
                        key: "ChiTietHoaDonNos",
                        reloadAsync: AppProviders.ChiTietHoaDonNos.ReloadAsync,
                        reloadUi: ReloadChiTietHoaDonNoUI,
                        friendlyNameForToast: "Chi tiết HĐ nợ");
                },

                ["CongViecNoiBo"] = async () =>
                {
                    await ExecuteWithFreshnessAsync(
                        key: "CongViecNoiBos",
                        reloadAsync: AppProviders.CongViecNoiBos.ReloadAsync,
                        reloadUi: ReloadCongViecNoiBoUI,
                        friendlyNameForToast: "Công việc nội bộ");
                },

                ["ChiTietHoaDonThanhToan"] = async () =>
                {
                    await ExecuteWithFreshnessAsync(
                        key: "ChiTietHoaDonThanhToans",
                        reloadAsync: AppProviders.ChiTietHoaDonThanhToans.ReloadAsync,
                        reloadUi: ReloadChiTietHoaDonThanhToanUI,
                        friendlyNameForToast: "Chi tiết HĐ thanh toán");
                },
            };

            // Thực thi action tương ứng tag
            if (loadActions.TryGetValue(tag, out var action))
            {
                await action();
            }
        }



        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var selectedTab = TabControl.SelectedItem as TabItem;
            if (selectedTab != null)
            {
                if (selectedTab.Tag.ToString() == "HoaDon")
                {
                    switch (e.Key)
                    {
                        case Key.Enter:
                        case Key.Space: // 🟟 thêm xử lý phím cách
                            if (_fullHoaDonList.Any(hd => hd.BaoDon)) // chỉ khi đang báo đơn
                            {
                                foreach (var hd in _fullHoaDonList.Where(x => x.BaoDon))
                                {
                                    hd.BaoDon = false;
                                    var api = new HoaDonApi();
                                    await api.UpdateSingleAsync(hd.Id, hd);
                                }
                                await AppProviders.HoaDons.ReloadAsync();
                                ReloadHoaDonUI();
                                AudioHelper.Stop();
                                e.Handled = true; // tránh Space làm scroll
                            }
                            break;

                        case Key.Escape:
                            EscButton_Click(this, new RoutedEventArgs());

                            break;
                        case Key.F1:
                            F1Button_Click(this, new RoutedEventArgs());
                            break;
                        case Key.F2:
                            F2Button_Click(this, new RoutedEventArgs());
                            break;
                        case Key.F3:
                            F3Button_Click(this, new RoutedEventArgs());
                            break;
                        case Key.F4:
                            F4Button_Click(this, new RoutedEventArgs());
                            break;
                        case Key.F5:
                            F5Button_Click(this, new RoutedEventArgs());
                            break;
                        case Key.F6:
                            F6Button_Click(this, new RoutedEventArgs());
                            break;
                        case Key.F7:
                            F7Button_Click(this, new RoutedEventArgs());
                            break;
                        case Key.F8:
                            F8Button_Click(this, new RoutedEventArgs());
                            break;
                        case Key.F9:
                            F9Button_Click(this, new RoutedEventArgs());
                            break;
                        case Key.F10:
                            F10Button_Click(this, new RoutedEventArgs());
                            break;
                        case Key.F12:
                            F12Button_Click(this, new RoutedEventArgs());
                            break;
                        case Key.Delete:
                            DelButton_Click(this, new RoutedEventArgs());
                            break;
                    }
                }
                else
                if (selectedTab.Tag.ToString() == "ChiTietHoaDonNo")
                {
                    switch (e.Key)
                    {
                        case Key.F1:
                            F1aButton_Click(this, new RoutedEventArgs());
                            break;
                        case Key.F4:
                            F4aButton_Click(this, new RoutedEventArgs());
                            break;
                        case Key.F5:
                            F5aButton_Click(this, new RoutedEventArgs());
                            break;
                    }
                }
            }
        }
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
                LoaiThanhToan = selected.Ngay == now.Date
                                ? "Trả nợ trong ngày"
                                : "Trả nợ qua ngày",
                GhiChu = selected.GhiChu,
                SoTien = selected.SoTienConLai,
            };
        }
        public static SolidColorBrush MakeBrush(Brush brush, double opacity = 1.0)
        {
            if (brush is SolidColorBrush solid)
            {
                var color = solid.Color;
                var newBrush = new SolidColorBrush(color);
                newBrush.Opacity = opacity; // 0.0 -> 1.0
                return newBrush;
            }

            // fallback nếu không phải SolidColorBrush
            return new SolidColorBrush(Colors.Transparent) { Opacity = opacity };
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

            await UpdateDashboardSummary(); // luôn refresh thống kê
        }
        private async Task SafeButtonHandlerAsync(
    Button button,
    Func<HoaDonDto?, Task> action,
    bool requireSelectedHoaDon = false)
        {
            HoaDonDto? selected = null;

            if (requireSelectedHoaDon)
            {
                if (HoaDonDataGrid.SelectedItem is not HoaDonDto hd)
                {
                    NotiHelper.Show("Vui lòng chọn hoá đơn!");
                    return;
                }
                selected = hd;
            }

            try
            {
                if (button != null) button.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;
                ProgressBar.Visibility = Visibility.Visible;
                await action(selected);

            }
            catch (Exception ex)
            {
                NotiHelper.ShowError($"Lỗi: {ex.Message}");
            }
            finally
            {
                await Task.Delay(100);
                Mouse.OverrideCursor = null;
                ProgressBar.Visibility = Visibility.Collapsed;

                if (button != null) button.IsEnabled = true;
            }
        }
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



        private async void F1aButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected)
            {
                NotiHelper.Show("Vui lòng chọn công nợ!");
                return;
            }
            if (selected.SoTienConLai == 0)
            {
                NotiHelper.Show("Công nợ đã thu đủ!");
                return;
            }

            var dto = TaoDtoTraNo(selected, Guid.Parse("0121FC04-0469-4908-8B9A-7002F860FB5C"));
            var window = new ChiTietHoaDonThanhToanEdit(dto)
            {
                Owner = this,
                Width = ActualWidth,
                Height = ActualHeight
            };
            window.PhuongThucThanhToanComboBox.IsEnabled = false;

            if (window.ShowDialog() == true)
                await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
        }
        private async void F4aButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected)
            {
                NotiHelper.Show("Vui lòng chọn công nợ!");
                return;
            }
            if (selected.SoTienConLai == 0)
            {
                NotiHelper.Show("Công nợ đã thu đủ!");
                return;
            }

            var dto = TaoDtoTraNo(selected, Guid.Parse("2cf9a88f-3bc0-4d4b-940d-f8ffa4affa02"));
            var window = new ChiTietHoaDonThanhToanEdit(dto)
            {
                Owner = this,
                Width = ActualWidth,
                Height = ActualHeight
            };
            window.PhuongThucThanhToanComboBox.IsEnabled = false;

            if (window.ShowDialog() == true)
                await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
        }
        private async void F5aButton_Click(object sender, RoutedEventArgs e)
        {
            return;
            if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected)
            {
                NotiHelper.Show("Vui lòng chọn công nợ!");
                return;
            }
            if (selected.SoTienConLai == 0)
            {
                NotiHelper.Show("Công nợ đã thu đủ!");
                return;
            }

            var dto = TaoDtoTraNo(selected, Guid.Parse("3d75dd9f-a5d3-491d-a316-6d5c9ff7e66c"));
            var window = new ChiTietHoaDonThanhToanEdit(dto)
            {
                Owner = this,
                Width = ActualWidth,
                Height = ActualHeight
            };
            window.PhuongThucThanhToanComboBox.IsEnabled = false;

            if (window.ShowDialog() == true)
                await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
        }


        private async void F2Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F2Button, async selected =>
            {
                var api = new HoaDonApi();
                var result = await api.GetByIdAsync(selected!.Id);

                if (!result.IsSuccess || result.Data == null)
                {
                    NotiHelper.ShowError($"Không thể tải chi tiết hóa đơn: {result.Message}");
                    return;
                }

                HoaDonPrinter.Print(result.Data);
            }, requireSelectedHoaDon: true);
        }
        private async void F3Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F3Button, async selected =>
            {
                var api = new HoaDonApi();
                var result = await api.GetByIdAsync(selected!.Id);

                if (!result.IsSuccess || result.Data == null)
                {
                    NotiHelper.ShowError($"Không thể tải chi tiết hóa đơn: {result.Message}");
                    return;
                }

                var (qrPng, message) = HoaDonPrinter.PrepareZaloTextAndQr_AlwaysCopy(result.Data);

                NotiHelper.Show("Đã copy, ctrl v để gửi!");
            }, requireSelectedHoaDon: true);
        }
        private async void F6Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F6Button, async _ =>
            {
                var uri = new Uri("pack://application:,,,/Images/viettin007.jpg");
                BitmapImage bitmap = new BitmapImage(uri);
                Clipboard.SetImage(bitmap);
                NotiHelper.Show("Đã copy, ctrl v để gửi!");
                await Task.CompletedTask;
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

                // Lấy tất cả file ảnh (jpg, png, jpeg…)
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
            await SafeButtonHandlerAsync(F9Button, async _ =>
            {
                if (HenGioStackPanel.Visibility != Visibility.Visible)
                {
                    GioCombo.SelectedIndex = DateTime.Now.Hour - 6;
                    PhutCombo.SelectedIndex = DateTime.Now.Minute / 10;
                    HenGioStackPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    HenGioStackPanel.Visibility = Visibility.Collapsed;
                }
                await Task.CompletedTask;
            });
        }

        private async void EscButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(EscButton, async selected =>
            {
                var confirm = MessageBox.Show(
         $"Nếu shipper là Khánh chọn YES\nNếu không phải chọn NO\nHuỷ bỏ chọn CANCEL",
         "QUAN TRỌNG:",
         MessageBoxButton.YesNoCancel,
         MessageBoxImage.Question
     );

                if (confirm == MessageBoxResult.Yes)
                {
                    // Người dùng chọn YES
                    selected!.NguoiShip = "Khánh";

                }
                else if (confirm == MessageBoxResult.No)
                {
                    // Người dùng chọn NO
                    selected!.NguoiShip = null;

                }
                else if (confirm == MessageBoxResult.Cancel)
                {
                    // Người dùng chọn CANCEL -> bỏ qua hành động
                    return;
                }



                selected!.NgayShip = DateTime.Now;
                var api = new HoaDonApi();
                var result = await api.UpdateSingleAsync(selected.Id, selected);


                if (!result.IsSuccess)
                    NotiHelper.ShowError($"Lỗi: {result.Message}");
                else
                {
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true);
                    F2Button_Click(this, new RoutedEventArgs());
                }



            }, requireSelectedHoaDon: true);
        }
        private async void F1Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F1Button, async selected =>
            {
                if (selected!.ConLai == 0) { NotiHelper.Show("Hoá đơn đã thu đủ!"); return; }
                if (selected.TrangThai.ToLower().Contains("nợ")) { NotiHelper.Show("Vui lòng thanh toán tại tab Công nợ!"); return; }

                var dto = TaoDtoThanhToan(selected, Guid.Parse("0121FC04-0469-4908-8B9A-7002F860FB5C"));
                var window = new ChiTietHoaDonThanhToanEdit(dto)
                {
                    Owner = this,
                    Width = ActualWidth,
                    Height = ActualHeight
                };
                window.PhuongThucThanhToanComboBox.IsEnabled = false;

                if (window.ShowDialog() == true)
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true);

            }, requireSelectedHoaDon: true);
        }
        private async void F4Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F4Button, async selected =>
            {
                if (selected!.ConLai == 0) { NotiHelper.Show("Hoá đơn đã thu đủ!"); return; }
                if (selected.TrangThai.ToLower().Contains("nợ")) { NotiHelper.Show("Vui lòng thanh toán tại tab Công nợ!"); return; }

                var dto = TaoDtoThanhToan(selected, Guid.Parse("2cf9a88f-3bc0-4d4b-940d-f8ffa4affa02"));
                var window = new ChiTietHoaDonThanhToanEdit(dto)
                {
                    Owner = this,
                    Width = ActualWidth,
                    Height = ActualHeight
                };
                window.PhuongThucThanhToanComboBox.IsEnabled = false;

                if (window.ShowDialog() == true)
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true);
            }, requireSelectedHoaDon: true);
        }
        private async void F5Button_Click(object sender, RoutedEventArgs e)
        {
            return;
            await SafeButtonHandlerAsync(F5Button, async selected =>
            {
                if (selected!.ConLai == 0) { NotiHelper.Show("Hoá đơn đã thu đủ!"); return; }
                if (selected.TrangThai.ToLower().Contains("nợ")) { NotiHelper.Show("Vui lòng thanh toán tại tab Công nợ!"); return; }

                var dto = TaoDtoThanhToan(selected, Guid.Parse("3d75dd9f-a5d3-491d-a316-6d5c9ff7e66c"));
                var window = new ChiTietHoaDonThanhToanEdit(dto)
                {
                    Owner = this,
                    Width = ActualWidth,
                    Height = ActualHeight
                };
                window.PhuongThucThanhToanComboBox.IsEnabled = false;

                if (window.ShowDialog() == true)
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true);
            }, requireSelectedHoaDon: true);
        }
        private async void F7Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F7Button, async selected =>
            {
                selected!.BaoDon = !selected.BaoDon;
                var api = new HoaDonApi();
                var result = await api.UpdateSingleAsync(selected.Id, selected);

                if (!result.IsSuccess)
                    NotiHelper.ShowError($"Lỗi: {result.Message}");
                else
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true);
            }, requireSelectedHoaDon: true);
        }
        private async void F8Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F8Button, async selected =>
            {
                selected!.UuTien = !selected.UuTien;
                var api = new HoaDonApi();
                var result = await api.UpdateSingleAsync(selected.Id, selected);

                if (!result.IsSuccess)
                    NotiHelper.ShowError($"Lỗi: {result.Message}");
                else
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true);
            }, requireSelectedHoaDon: true);
        }
        private async void OkHenGioButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(OkHenGio, async selected =>
            {
                int.TryParse(GioCombo.SelectedItem?.ToString(), out int gio);
                int.TryParse(PhutCombo.SelectedItem?.ToString(), out int phut);

                selected!.NgayHen = DateTime.Now.Date.AddHours(gio).AddMinutes(phut);

                var api = new HoaDonApi();
                var result = await api.UpdateSingleAsync(selected.Id, selected);

                if (!result.IsSuccess)
                    NotiHelper.ShowError($"Lỗi: {result.Message}");
                else
                {
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true);
                    HenGioStackPanel.Visibility = Visibility.Collapsed;
                }
            }, requireSelectedHoaDon: true);
        }
        private async void F12Button_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(F12Button, async selected =>
            {
                if (selected!.ConLai == 0) { NotiHelper.Show("Hoá đơn đã thu đủ!"); return; }
                if (selected.TrangThai.ToLower().Contains("nợ")) { NotiHelper.Show("Hoá đơn đã ghi nợ!"); return; }
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

                var window = new ChiTietHoaDonNoEdit(dto)
                {
                    Owner = this,
                    Width = ActualWidth,
                    Height = ActualHeight,
                    Background = MakeBrush((Brush)System.Windows.Application.Current.Resources["DangerBrush"], 0.8)
                };
                window.SoTienTextBox.IsReadOnly = true;

                if (window.ShowDialog() == true)
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadNo: true);
            }, requireSelectedHoaDon: true);
        }
        private async void DelButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(DelButton, async selected =>
            {
                var confirm = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xoá '{selected!.Ten}'?",
                    "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                var response = await ApiClient.DeleteAsync($"/api/HoaDon/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<HoaDonDto>>();

                if (result?.IsSuccess == true)
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
                else
                    NotiHelper.ShowError(result?.Message ?? "Không thể xoá.");
            }, requireSelectedHoaDon: true);
        }
        private async void AppButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(AppButton, async _ =>
            {
                var helper = new AppShippingHelperText("12122431577", "baothanh1991");
                var dto = await Task.Run(() => helper.GetFirstOrderPopup());

                var window = new HoaDonEdit(dto)
                {
                    Width = this.ActualWidth,
                    Height = this.ActualHeight,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (window.ShowDialog() == true)
                {
                    // ✅ bổ sung reloadNo để thống kê công nợ luôn được cập nhật
                    await ReloadAfterHoaDonChangeAsync(
                        reloadHoaDon: true,
                        reloadThanhToan: true,
                        reloadNo: true
                    );
                }

            });
        }
        private async void HisButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(HisButton, async selected =>
            {
                if (selected!.KhachHangId == null)
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
            }, requireSelectedHoaDon: true);
        }








        // 🟟 Dọn timer khi đóng form
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppShippingHelperText.DisposeDriver();

            _baoDonTimer.Stop();
            _congViecTimer.Stop();
        }
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
                        icon.Foreground = (Brush)System.Windows.Application.Current.Resources["DangerBrush"];
                    }
                    break;
                case "Tại Chỗ":
                    if (hd.TrangThai == "Chưa thu")
                    {
                        icon.Icon = IconChar.Chair;
                        icon.Foreground = (Brush)System.Windows.Application.Current.Resources["SuccessBrush"];
                    }
                    break;
                case "Mv":
                    if (hd.TrangThai == "Chưa thu")
                    {
                        icon.Icon = IconChar.BagShopping;
                        icon.Foreground = (Brush)System.Windows.Application.Current.Resources["WarningBrush"];
                    }
                    break;
                case "Ship":
                    icon.Icon =
                        hd.NguoiShip == "Khánh" ?
                        IconChar.Motorcycle : hd.NgayShip == null ? IconChar.HourglassHalf : IconChar.Truck;
                    icon.Foreground =
                        hd.NguoiShip == "Khánh" ?
                    (Brush)System.Windows.Application.Current.Resources["DangerBrush"] :
                    hd.NgayShip == null ?
                    (Brush)System.Windows.Application.Current.Resources["PrimaryBrush"] :
                    (Brush)System.Windows.Application.Current.Resources["DarkBrush"];
                    break;
                default:
                    icon.Icon = IconChar.Circle; // fallback
                    icon.Foreground = (Brush)System.Windows.Application.Current.Resources["SecondaryBrush"];
                    break;
            }

            // Nhấp nháy (Opacity)
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




        private async void AddAIButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(AppButton, async _ =>
            {
                string input = SearchHoaDonTextBox.Text.Trim();
                if (string.IsNullOrEmpty(input)) input = Clipboard.GetText().Trim();
                if (string.IsNullOrEmpty(input)) return;
                // ✅ Dùng DTO giống shipper
                var dto = await _quickOrder.BuildHoaDonFromQuickAsync(input); // yêu cầu bạn đã thêm method này trong QuickOrderService
                if (dto == null || dto.ChiTietHoaDons == null || dto.ChiTietHoaDons.Count == 0)
                {
                    MessageBox.Show("❌ Không nhận diện được món nào.");
                    DiscordService.SendAsync(DiscordEventType.Admin, input);
                    return;
                }

                // tuỳ flow: giữ nguyên như bạn đang dùng
                dto.PhanLoai = "Ship";

                var window = new HoaDonEdit(dto)
                {
                    Width = this.ActualWidth,
                    Height = this.ActualHeight,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (window.ShowDialog() == true)
                {
                    await ReloadAfterHoaDonChangeAsync(
                        reloadHoaDon: true,
                        reloadThanhToan: true,
                        reloadNo: true
                    );
                }

                SearchHoaDonTextBox.Clear();

            });
        }
        private void SearchHoaDonTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchHoaDonTextBox.Height = 32;
        }


        private void TimKiemNhanhThanhToanButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
                SearchChiTietHoaDonThanhToanTextBox.Text = bt.Tag.ToString();
        }
        private void TimKiemNhanhCongNoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
            {
                if (bt.Tag.ToString() == "hôm nay")
                    SearchChiTietHoaDonNoTextBox.Text = DateTime.Today.ToString("dd-MM-yyyy");
                else if (bt.Tag.ToString() == "hôm qua")
                    SearchChiTietHoaDonNoTextBox.Text = DateTime.Today.AddDays(-1).ToString("dd-MM-yyyy");
            }
        }
    }
}

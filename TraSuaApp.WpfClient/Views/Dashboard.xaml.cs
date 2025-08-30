using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;
using TraSuaApp.WpfClient.Services;
using TraSuaApp.WpfClient.SettingsViews;

namespace TraSuaApp.WpfClient.Views
{

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
    public partial class Dashboard : Window
    {
        private MediaPlayer _baoDonPlayer;
        private DispatcherTimer _mainTimer;

        public Dashboard()
        {
            InitializeComponent();
            _baoDonPlayer = new MediaPlayer();
            var uri = new Uri("pack://application:,,,/Resources/ring3.wav"); //
            _baoDonPlayer.Open(uri); // đường dẫn file
            _baoDonPlayer.Volume = 0.8; // âm lượng 0.0 → 1.0
            _baoDonPlayer.MediaEnded += (s, e) =>
            {
                _baoDonPlayer.Position = TimeSpan.Zero;
                _baoDonPlayer.Play();
            };
            _mainTimer = new DispatcherTimer();
            _mainTimer.Interval = TimeSpan.FromSeconds(10); // tick mỗi 10 giây
            _mainTimer.Tick += async (s, e) => await MainTimer_Tick();
            _mainTimer.Start();

            Loaded += Dashboard_Loaded;

            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            GenerateMenu("Admin", AdminMenu);
            GenerateMenu("HoaDon", HoaDonMenu);
            GenerateMenu("Settings", SettingsMenu);
            for (int h = 6; h < 22; h++) GioCombo.Items.Add(h.ToString("D2"));
            for (int m = 0; m < 60; m += 10) PhutCombo.Items.Add(m.ToString("D2")); // bước 5 phút
        }
        private async Task MainTimer_Tick()
        {
            // 🟟 Báo đơn
            if (_fullHoaDonList.Any(hd => hd.BaoDon))
            {
                if (_baoDonPlayer.Position == TimeSpan.Zero || _baoDonPlayer.CanPause)
                {
                    _baoDonPlayer.Play();
                }
            }
            else
            {
                _baoDonPlayer.Stop();
            }

            // 🟟 Refresh giờ hiển thị (mỗi 1 phút mới cần)
            if (DateTime.Now.Second < 10) // tick đầu phút
            {
                foreach (var item in _fullHoaDonList)
                    item.RefreshGioHienThi();
            }

            // 🟟 Check đơn hẹn giờ
            var dsDenHan = _fullHoaDonList
                .Where(h => h.NgayHen.HasValue && h.NgayHen.Value <= DateTime.Now)
                .ToList();

            foreach (var hd in dsDenHan)
            {
                // ⚠️ Không dùng MessageBox spam liên tục
                MessageBox.Show($"⏰ Đến giờ hẹn: {hd.Ten} ({hd.TongTien:N0}đ)");

                hd.NgayHen = null;
                var api = new HoaDonApi();
                await api.UpdateSingleAsync(hd.Id, hd);
            }

            // 🟟 Công việc nội bộ (mỗi 15 phút mới đọc lại)
            if (DateTime.Now.Minute % 15 == 0 && AppProviders.CongViecNoiBos != null)
            {
                var congViec = _fullCongViecNoiBoList
                    .Where(cv => !cv.DaHoanThanh)
                    .OrderBy(x => x.NgayGio)
                    .FirstOrDefault();

                if (congViec != null)
                    await TTSHelper.DownloadAndPlayGoogleTTSAsync(congViec.Ten);
            }
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

        private async void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                today = DateTime.Today;

                // 🟟 Khởi tạo providers
                await BindAllProviders();

                await AppProviders.ReloadAllAsync();   // 🟟 Gọi reload tất cả
                await UpdateDashboardSummary();
                await ReloadThongKeUI();
            }
            catch (Exception ex)
            {
                NotiHelper.Show("Lỗi tải dashboard: " + ex.Message);
            }
        }
        private async Task BindProviderAsync(Func<bool> waitCondition, Action<Action> subscribe, Action reloadAction, string name)
        {
            await WaitForDataAsync(waitCondition);
            subscribe(() =>
            {
                Debug.WriteLine($"{DateTime.Now:T} - {name} changed");
                reloadAction();
                ScheduleUpdateDashboardSummary();
            });
        }

        private DispatcherTimer _updateSummaryTimer = new();
        private List<ChiTietHoaDonDto> _fullChiTietHoaDonList = new();
        private CancellationTokenSource _cts = new();

        private void ScheduleUpdateDashboardSummary()
        {
            if (_updateSummaryTimer == null)
            {
                _updateSummaryTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500) // debounce 0.5s
                };
                _updateSummaryTimer.Tick += async (s, e) =>
                {
                    _updateSummaryTimer.Stop();
                    await UpdateDashboardSummary();
                };
            }

            _updateSummaryTimer.Stop();
            _updateSummaryTimer.Start();
        }
        private async Task BindAllProviders()
        {
            var providers = new List<(Func<bool> wait, Action<Action> subscribe, Action reload, string name)>
    {
        (() => AppProviders.HoaDons?.Items != null,
         h => AppProviders.HoaDons.OnChanged += h,
         ReloadHoaDonUI,
         "HoaDons"),

        (() => AppProviders.CongViecNoiBos?.Items != null,
         h => AppProviders.CongViecNoiBos.OnChanged += h,
         ReloadCongViecNoiBoUI,
         "CongViecNoiBos"),

        (() => AppProviders.ChiTietHoaDonNos?.Items != null,
         h => AppProviders.ChiTietHoaDonNos.OnChanged += h,
         ReloadChiTietHoaDonNoUI,
         "ChiTietHoaDonNos"),

        (() => AppProviders.ChiTietHoaDonThanhToans?.Items != null,
         h => AppProviders.ChiTietHoaDonThanhToans.OnChanged += h,
         ReloadChiTietHoaDonThanhToanUI,
         "ChiTietHoaDonThanhToans"),

        (() => AppProviders.ChiTieuHangNgays?.Items != null,
         h => AppProviders.ChiTieuHangNgays.OnChanged += h,
         ReloadChiTieuHangNgayUI,
         "ChiTieuHangNgays"),
    };

            foreach (var (wait, subscribe, reload, name) in providers)
            {
                await BindProviderAsync(wait, subscribe, reload, name);
            }
        }

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







        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
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
            this.Close();
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
        private async void XoaCongViecNoiBoButton_Click(object sender, RoutedEventArgs e)
        {
            if (CongViecNoiBoDataGrid.SelectedItem is not CongViecNoiBoDto selected)
                return;
            var confirm = MessageBox.Show(
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
            selected.LastModified = updated.LastModified;

            SearchCongViecNoiBoTextBox.Text = "";
            ReloadCongViecNoiBoUI();
            SearchCongViecNoiBoTextBox.Focus();
        }
        private void SearchCongViecNoiBoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceCongViec.Debounce(300, ApplyCongViecNoiBoFilter);
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
        private void ReloadCongViecNoiBoUI()
        {
            _fullCongViecNoiBoList = AppProviders.CongViecNoiBos.Items
                .OrderBy(x => x.DaHoanThanh)
                .ThenByDescending(x => x.LastModified)
                .ToList();

            ApplyCongViecNoiBoFilter();
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
                SoTien = selected.ConLai,
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
        private void SearchChiTietHoaDonNoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceChiTietNo.Debounce(300, ApplyChiTietHoaDonNoFilter);
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
            tongTien = sourceList.Sum(x => x.ConLai);

            TongTienChiTietHoaDonNoTextBlock.Header = $"{tongTien:N0} đ";



        }
        private void ReloadChiTietHoaDonNoUI()
        {
            _fullChiTietHoaDonNoList = AppProviders.ChiTietHoaDonNos.Items
                .Where(x => x.ConLai > 0)
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.LastModified)
                .ToList();
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
        private void SearchChiTietHoaDonThanhToanTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceThanhToan.Debounce(300, ApplyChiTietHoaDonThanhToanFilter);
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
                sourceList = _fullChiTietHoaDonThanhToanList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
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
        private void ReloadChiTietHoaDonThanhToanUI()
        {
            _fullChiTietHoaDonThanhToanList = AppProviders.ChiTietHoaDonThanhToans.Items
             .Where(x => !x.IsDeleted && x.Ngay == today)
                .OrderByDescending(x => x.NgayGio)
                .ToList();
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
        private void SearchChiTieuHangNgayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceChiTieu.Debounce(300, ApplyChiTieuHangNgayFilter);
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
        private void ReloadChiTieuHangNgayUI()
        {
            _fullChiTieuHangNgayList = AppProviders.ChiTieuHangNgays.Items
                  .Where(x => !x.IsDeleted)
                .Where(x => x.Ngay == today)
                .OrderBy(x => x.BillThang)
                .ThenByDescending(x => x.NgayGio)
                .ToList();
            ApplyChiTieuHangNgayFilter();
        }




        private void RenderSummary(StackPanel panel, string title, decimal total, IEnumerable<(string Label, decimal Value)> items)
        {
            panel.Children.Clear();

            // Dòng tổng trên cùng
            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run($"{title}\n"));
            totalTextBlock.Inlines.Add(new Run($"{total:N0} đ") { FontWeight = FontWeights.Bold });
            panel.Children.Add(totalTextBlock);

            // Các dòng chi tiết
            foreach (var item in items)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var labelText = new TextBlock
                {
                    Text = $"{item.Label}:",
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var valueText = new TextBlock
                {
                    Text = $"{item.Value:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(labelText, 0);
                Grid.SetColumn(valueText, 1);

                grid.Children.Add(labelText);
                grid.Children.Add(valueText);

                panel.Children.Add(grid);
            }
        }
        private async Task LoadDoanhThuDynamic()
        {
            await WaitForDataAsync(() => AppProviders.HoaDons?.Items != null);

            var groups = AppProviders.HoaDons.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .GroupBy(x => x.PhanLoai)
                .Select(g => (Label: g.Key ?? "Khác", Value: g.Sum(x => x.ThanhTien)))
                .OrderByDescending(g => g.Value)
                .ToList();

            RenderSummary(DoanhThuStackPanel, "Doanh thu", groups.Sum(x => x.Value), groups);
        }
        private async Task LoadDaThuDynamic()
        {
            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonThanhToans?.Items != null);

            var groups = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => x.LoaiThanhToan.ToLower().Contains("trong ngày"))
                .GroupBy(x => x.TenPhuongThucThanhToan)
                .Select(g => (Label: g.Key ?? "Khác", Value: g.Sum(x => x.SoTien)))
                .OrderByDescending(g => g.Value)
                .ToList();

            RenderSummary(DaThuStackPanel, "Đã thu", groups.Sum(x => x.Value), groups);
        }
        private async Task LoadChiTieuDynamic()
        {
            await WaitForDataAsync(() => AppProviders.ChiTieuHangNgays?.Items != null);

            var groups = AppProviders.ChiTieuHangNgays.Items
                .Where(x => !x.IsDeleted && x.Ngay == today && !x.BillThang)
                .GroupBy(x => x.Ten)
                .Select(g => (Label: g.Key ?? "Khác", Value: g.Sum(x => x.ThanhTien)))
                .OrderByDescending(g => g.Value)
                .ToList();

            RenderSummary(ChiTieuStackPanel, "Chi tiêu", groups.Sum(x => x.Value), groups);
        }
        private async Task LoadChuaThuDynamic()
        {
            await WaitForDataAsync(() => AppProviders.HoaDons?.Items != null);

            var hoaDonChuaThu = AppProviders.HoaDons.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => x.ConLai > 0 && x.TrangThai == "Chưa thu")
                .OrderByDescending(x => x.TongTien)
                .Select(hd => (Label: hd.TenKhachHangText ?? "Khách lạ", Value: hd.ConLai))
                .ToList();

            RenderSummary(ChuaThuStackPanel, "Chưa thu", hoaDonChuaThu.Sum(x => x.Value), hoaDonChuaThu);
        }
        private async Task LoadCongNoDynamic()
        {
            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonNos?.Items != null);

            var groups = AppProviders.ChiTietHoaDonNos.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .GroupBy(x => x.Ten)
                .Select(g => (Label: g.Key ?? "Khách lạ", Value: g.Sum(x => x.ConLai)))
                .OrderByDescending(g => g.Value)
                .ToList();

            RenderSummary(CongNoStackPanel, "Ghi nợ", groups.Sum(x => x.Value), groups);
        }
        private async Task LoadTraNoBankDynamic()
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

            RenderSummary(TraNoBankStackPanel, "Trả nợ bank", groups.Sum(x => x.Value), groups);
        }
        private async Task LoadTraNoTienDynamic()
        {
            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonThanhToans?.Items != null);

            var groups = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => x.LoaiThanhToan.ToLower() == "trả nợ qua ngày")
                .Where(x => x.TenPhuongThucThanhToan?.ToLower() == "tiền mặt")
                .GroupBy(x => x.KhachHangId)
                .Select(g => (Label: g.FirstOrDefault()?.Ten ?? "Khách lạ", Value: g.Sum(x => x.SoTien)))
                .OrderByDescending(g => g.Value)
                .ToList();

            RenderSummary(TraNoTienStackPanel, "Trả nợ tiền", groups.Sum(x => x.Value), groups);
        }
        private async Task LoadMangVeDynamic()
        {
            MangVeStackPanel.Children.Clear();

            // Tính tổng tất cả
            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonThanhToans?.Items != null);

            decimal tongTienMat = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => x.LoaiThanhToan.ToLower() == "trong ngày")
                .Where(x => x.TenPhuongThucThanhToan?.ToLower() == "tiền mặt")
                .Sum(x => x.SoTien);
            decimal tongChiTieu = AppProviders.ChiTieuHangNgays.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => !x.BillThang)
                .Sum(x => x.ThanhTien);
            decimal tongChuaThu = AppProviders.HoaDons.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => x.ConLai > 0 && x.TrangThai?.ToLower() == "chưa thu")
                .Sum(g => g.ConLai);
            decimal tongTatCa = tongTienMat - tongChiTieu + tongChuaThu;

            decimal tongTraNo = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x =>
            x.LoaiThanhToan?.ToLower().Contains("trả nợ") == true
            &&
            x.TenPhuongThucThanhToan?.ToLower().Contains("tiền mặt") == true
            )
            .Sum(x => x.SoTien);
            // Hiển thị số tiền mặt mang về (tongTatCa) ở trên cùng
            var mangVeTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mangVeTextBlock.Inlines.Add(new Run("Mang về\n"));
            mangVeTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            MangVeStackPanel.Children.Add(mangVeTextBlock);

            // Hiển thị tổng tiền mặt
            var tongTienMatGrid = new Grid();
            tongTienMatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tongTienMatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tongTienMatLabel = new TextBlock
            {
                Text = "Tiền mặt:",
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var tongTienMatValue = new TextBlock
            {
                Text = $"{tongTienMat:N0} đ",
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
            };

            Grid.SetColumn(tongTienMatLabel, 0);
            Grid.SetColumn(tongTienMatValue, 1);
            tongTienMatGrid.Children.Add(tongTienMatLabel);
            tongTienMatGrid.Children.Add(tongTienMatValue);

            MangVeStackPanel.Children.Add(tongTienMatGrid);

            // Hiển thị tổng chi tiêu
            var tongChuaThuGrid = new Grid();
            tongChuaThuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tongChuaThuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tongChuaThuLabel = new TextBlock
            {
                Text = "Chưa thu:",
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var tongChuaThuValue = new TextBlock
            {
                Text = $"{tongChuaThu:N0} đ",
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
            };

            Grid.SetColumn(tongChuaThuLabel, 0);
            Grid.SetColumn(tongChuaThuValue, 1);
            tongChuaThuGrid.Children.Add(tongChuaThuLabel);
            tongChuaThuGrid.Children.Add(tongChuaThuValue);

            MangVeStackPanel.Children.Add(tongChuaThuGrid);




            // Hiển thị tổng chi tiêu
            var tongChiTieuGrid = new Grid();
            tongChiTieuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tongChiTieuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tongChiTieuLabel = new TextBlock
            {
                Text = "Chi tiêu:",
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var tongChiTieuValue = new TextBlock
            {
                Text = $"{-tongChiTieu:N0} đ",
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
            };

            Grid.SetColumn(tongChiTieuLabel, 0);
            Grid.SetColumn(tongChiTieuValue, 1);
            tongChiTieuGrid.Children.Add(tongChiTieuLabel);
            tongChiTieuGrid.Children.Add(tongChiTieuValue);

            MangVeStackPanel.Children.Add(tongChiTieuGrid);

            var tongThucTeGrid = new Grid();
            tongThucTeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tongThucTeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tongThucTeLabel = new TextBlock
            {
                Text = "\nKiểm tiền:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,

                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var tongThucTeValue = new TextBlock
            {
                Text = $"\n{tongTatCa - tongChuaThu:N0} đ",
                FontWeight = FontWeights.Bold,
                FontSize = 14,

                TextAlignment = TextAlignment.Right
            };

            Grid.SetColumn(tongThucTeLabel, 0);
            Grid.SetColumn(tongThucTeValue, 1);
            tongThucTeGrid.Children.Add(tongThucTeLabel);
            tongThucTeGrid.Children.Add(tongThucTeValue);

            MangVeStackPanel.Children.Add(tongThucTeGrid);

            // Hiển thị tổng tra no
            var tongTraNoGrid = new Grid();
            tongTraNoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tongTraNoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tongTraNoLabel = new TextBlock
            {
                Text = "Trả nợ tiền:",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var tongTraNoValue = new TextBlock
            {
                Text = $"{tongTraNo:N0} đ",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
            };

            Grid.SetColumn(tongTraNoLabel, 0);
            Grid.SetColumn(tongTraNoValue, 1);
            tongTraNoGrid.Children.Add(tongTraNoLabel);
            tongTraNoGrid.Children.Add(tongTraNoValue);

            MangVeStackPanel.Children.Add(tongTraNoGrid);

            var copyButton = new Button
            {
                Content = "Copy",
                Foreground = Brushes.WhiteSmoke,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(20),
                Style = (Style)FindResource("AddButtonStyle")
            };

            copyButton.Click += (s, e) =>
            {
                copyButton.Visibility = Visibility.Collapsed;

                // Lưu kích thước thật
                var size = new System.Windows.Size(MangVeBorder.ActualWidth, MangVeBorder.ActualHeight);

                // RenderTargetBitmap với DPI cao để ảnh mịn
                double dpi = 192; // gấp đôi 96 để mịn hơn
                var rtb = new RenderTargetBitmap(
                    (int)(size.Width * dpi / 96),
                    (int)(size.Height * dpi / 96),
                    dpi, dpi,
                    PixelFormats.Pbgra32);

                // Vẽ nền để tránh bị trong suốt/mất màu
                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(new VisualBrush(MangVeBorder), null, new Rect(size));
                }
                rtb.Render(dv);

                // Copy vào clipboard
                Clipboard.SetImage(rtb);

                copyButton.Visibility = Visibility.Visible;
            };
            MangVeStackPanel.Children.Add(copyButton);

        }
        private async Task UpdateDashboardSummary()
        {
            try
            {
                // Load các panel
                await LoadDoanhThuDynamic();
                await LoadChuaThuDynamic();
                await LoadDaThuDynamic();       // ✅ chờ đúng cách
                await LoadTraNoTienDynamic();
                await LoadTraNoBankDynamic();
                await LoadChiTieuDynamic();
                await LoadMangVeDynamic();
                await LoadCongNoDynamic();

                // Nếu tab thống kê đang mở thì reload luôn thống kê
                var selectedTab = TabControl.SelectedItem as TabItem;
                if (selectedTab?.Tag?.ToString() == "ThongKeHomNay")
                {
                    await ReloadThongKeUI();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UpdateDashboardSummary lỗi: " + ex.Message);
            }
        }
        private async Task ReloadThongKeUI()
        {
            try
            {
                // Gọi 2 API cùng lúc
                var homNayTask = ApiClient.GetAsync("/api/dashboard/homnay");
                var duBaoTask = ApiClient.GetAsync("/api/dashboard/dubao");

                await Task.WhenAll(homNayTask, duBaoTask);

                // Đọc kết quả trả về
                var dashboard = await homNayTask.Result.Content.ReadFromJsonAsync<DashboardDto>();
                if (dashboard != null)
                {
                    BanNhieuGrid.ItemsSource = dashboard.TopSanPhams ?? new List<DashboardTopSanPhamDto>();
                }

                var dashboard2 = await duBaoTask.Result.Content.ReadFromJsonAsync<DashboardDto>();
                if (dashboard2 != null)
                {
                    DuDoanDoanhThu.Text = dashboard2.PredictedPeak ?? "Không có dữ liệu";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi tải thống kê: " + ex.Message);
            }
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
                await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true, reloadThanhToan: true, reloadNo: true);
        }

        private void AddTaiChoButton_Click(object sender, RoutedEventArgs e)
            => OpenHoaDonWithPhanLoai("Tại Chỗ");

        private void AddMuaVeButton_Click(object sender, RoutedEventArgs e)
            => OpenHoaDonWithPhanLoai("MV");

        private void AddShipButton_Click(object sender, RoutedEventArgs e)
            => OpenHoaDonWithPhanLoai("Ship");

        private void AddAppButton_Click(object sender, RoutedEventArgs e)
            => OpenHoaDonWithPhanLoai("App");



        private readonly DebounceDispatcher _debounceHoaDon = new();
        private readonly DebounceDispatcher _debounceChiTietHoaDon = new();
        private readonly DebounceDispatcher _debounceCongViec = new();
        private readonly DebounceDispatcher _debounceChiTietNo = new();
        private readonly DebounceDispatcher _debounceThanhToan = new();
        private readonly DebounceDispatcher _debounceChiTieu = new();

        private async void HoaDonDataGrid_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Hủy tác vụ cũ nếu có
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                // Reset UI
                SearchChiTietHoaDonTextBox.Visibility = Visibility.Collapsed;
                TongSoSanPhamTextBlock.Visibility = Visibility.Visible;
                TongSoSanPhamTextBlock.Text = string.Empty;
                ChiTietHoaDonListBox.ItemsSource = null;
                ChiTietHoaDonListBox.Background = Brushes.WhiteSmoke;
                TongNoKhachHangTextBlock.Text = "0 ₫";

                if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
                    return;

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
                    }
                }

                // Cập nhật UI
                ChiTietHoaDonListBox.ItemsSource = hd.ChiTietHoaDons;
                ChiTietHoaDonListBox.Background = hd.TongNoKhachHang > 0 ? Brushes.IndianRed : Brushes.WhiteSmoke;
                TongNoKhachHangTextBlock.Text = $"Công nợ: {hd.TongNoKhachHang:N0} ₫\nTổng: {hd.TongNoKhachHang + hd.ConLai:N0} ₫";
                TongSoSanPhamTextBlock.Text = hd.ChiTietHoaDons.Sum(x => x.SoLuong).ToString("N0");

                // Đọc lần lượt từng sản phẩm
                foreach (var ct in hd.ChiTietHoaDons)
                {
                    if (token.IsCancellationRequested)
                        return; // dừng ngay nếu đã chuyển hóa đơn khác

                    await TTSHelper.DownloadAndPlayGoogleTTSAsync(ct.TenSanPham);
                    if (!string.IsNullOrEmpty(ct.NoteText))
                    {
                        await Task.Delay(100);
                        await TTSHelper.DownloadAndPlayGoogleTTSAsync(ct.NoteText.Replace("#", ""));
                    }
                    await Task.Delay(300, token); // delay có thể bị hủy
                }
            }
            catch (OperationCanceledException)
            {
                // Bị hủy -> bỏ qua, không báo lỗi
            }
            catch (Exception ex)
            {
                NotiHelper.ShowError($"Lỗi: {ex.Message}");

            }
        }
        //private void ShowError(string message)
        //{
        //    NotiHelper.Show($"Lỗi: {message}");
        //}
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
        private void SearchHoaDonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceHoaDon.Debounce(300, ApplyHoaDonFilter);
        }
        private void ApplyHoaDonFilter()
        {
            string keyword = SearchHoaDonTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
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

            // Sắp xếp: các hoá đơn "Chưa thu" lên đầu
            sourceList = sourceList
        .OrderBy(x => x.TrangThai switch
        {
            "Chưa thu" => 0,        // Ưu tiên cao nhất
            "Thu một phần" => 1,    // Ưu tiên kế tiếp
            _ => 2                  // Các trạng thái khác
        })
        .ThenByDescending(x => x.UuTien)
        .ThenByDescending(x => x.NgayGio)
        .ToList();
            // Gán số thứ tự
            int stt = 1;
            foreach (var item in sourceList)
            {
                item.Stt = stt++;
            }

            HoaDonDataGrid.ItemsSource = sourceList;
            tongTien = sourceList.Sum(x => x.ThanhTien);
            TongTienHoaDonTextBlock.Header = $"{tongTien:N0} đ";
        }
        private void ReloadHoaDonUI()
        {
            _fullHoaDonList = AppProviders.HoaDons.Items
            .Where(x => !x.IsDeleted
         && (x.Ngay == today
             || x.TrangThai == "Chưa thu"
             || x.TrangThai == "Thu một phần"))
                .OrderByDescending(x => x.NgayGio)
                .ToList();
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
        private void SearchChiTietHoaDonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceChiTietHoaDon.Debounce(300, () =>
            {
                if (_fullChiTietHoaDonList == null) return;
                string keyword = SearchChiTietHoaDonTextBox.Text.Trim().ToLower();
                decimal tongTien = 0;
                List<ChiTietHoaDonDto> sourceList;

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    sourceList = _fullChiTietHoaDonList;
                }
                else
                {
                    sourceList = _fullChiTietHoaDonList
                        .Where(x => x.TimKiem.ToLower().Contains(keyword))
                        .ToList();
                }

                // Gán số thứ tự
                int stt = 1;
                foreach (var item in sourceList)
                {
                    item.Stt = stt++;
                }

                ChiTietHoaDonListBox.ItemsSource = sourceList;
                tongTien = sourceList.Sum(x => x.ThanhTien);
            });
        }

        string oldConn = "Server=192.168.1.85;Database=DennCoffee;user=sa;password=baothanh1991;TrustServerCertificate=True";
        string newConn = "Server=.;Database=TraSuaAppDb;Trusted_Connection=True;TrustServerCertificate=True";
        private DateTime today;

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var importer = new KhachHangImporter(oldConn, newConn);
            await importer.ImportAsync();

            var importer2 = new HoaDonImporter(oldConn, newConn);
            await importer2.ImportTodayAsync();
        }

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is not TabControl tabControl) return;

            var selectedTab = tabControl.SelectedItem as TabItem;
            if (selectedTab == null) return;

            var tag = selectedTab.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            ThongBaoTextBlock.Text = null;

            // Map tag → action load lại dữ liệu
            var loadActions = new Dictionary<string, Func<Task>>
            {
                ["ThongKeHomNay"] = async () =>
                {
                    var result1 = await ApiClient.GetAsync("/api/dashboard/homnay");
                    var dashboard1 = await result1.Content.ReadFromJsonAsync<DashboardDto>();
                    if (dashboard1 != null)
                        BanNhieuGrid.ItemsSource = dashboard1.TopSanPhams;

                    var result2 = await ApiClient.GetAsync("/api/dashboard/dubao");
                    var dashboard2 = await result2.Content.ReadFromJsonAsync<DashboardDto>();
                    if (dashboard2 != null)
                        DuDoanDoanhThu.Text = dashboard2.PredictedPeak ?? "Không có dữ liệu";
                },

                ["HoaDon"] = async () =>
                {
                    await AppProviders.HoaDons.ReloadAsync();   // ✅ luôn reload provider
                    ReloadHoaDonUI();                           // ✅ refresh UI
                },

                ["ChiTieuHangNgay"] = async () =>
                {
                    await AppProviders.ChiTieuHangNgays.ReloadAsync();
                    ReloadChiTieuHangNgayUI();
                },

                ["ChiTietHoaDonNo"] = async () =>
                {
                    await AppProviders.ChiTietHoaDonNos.ReloadAsync();
                    ReloadChiTietHoaDonNoUI();
                },

                ["CongViecNoiBo"] = async () =>
                {
                    await AppProviders.CongViecNoiBos.ReloadAsync();
                    ReloadCongViecNoiBoUI();
                },

                ["ChiTietHoaDonThanhToan"] = async () =>
                {
                    await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();
                    ReloadChiTietHoaDonThanhToanUI();
                }
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
                                _baoDonPlayer.Stop();
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
                SoTien = selected.ConLai,
            };
        }
        public static SolidColorBrush MakeBrush(SolidColorBrush brush, double opacity = 1.0)
        {
            var color = brush.Color;
            var newBrush = new SolidColorBrush(color);
            newBrush.Opacity = opacity; // 0.0 -> 1.0
            return newBrush;
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
            if (selected.ConLai == 0)
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
            if (selected.ConLai == 0)
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
            if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected)
            {
                NotiHelper.Show("Vui lòng chọn công nợ!");
                return;
            }
            if (selected.ConLai == 0)
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

                HoaDonPrinter.Copy(result.Data);
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
                selected!.NgayShip = DateTime.Now;
                var api = new HoaDonApi();
                var result = await api.UpdateSingleAsync(selected.Id, selected);

                if (!result.IsSuccess)
                    NotiHelper.ShowError($"Lỗi: {result.Message}");
                else
                    await ReloadAfterHoaDonChangeAsync(reloadHoaDon: true);
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

                var dto = new ChiTietHoaDonNoDto
                {
                    Ngay = DateTime.Now.Date,
                    NgayGio = DateTime.Now,
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
                    Background = MakeBrush(Brushes.IndianRed, 0.8)
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
    }
}

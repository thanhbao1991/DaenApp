using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
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
                NotiHelper.Show($"⏰ Đến giờ hẹn: {hd.Ten} ({hd.TongTien:N0}đ)");

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
                now = DateTime.Today.AddDays(-1);
                await BindAllProviders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dashboard: " + ex.Message);
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

        private DispatcherTimer _updateSummaryTimer;

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
                  && t.FullName.Contains(loai)
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
                    MessageBox.Show($"Không tìm thấy form: {tag}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Lỗi mở form '{tag}': {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Lỗi: {result.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
            ApplyCongViecNoiBoFilter();
        }
        private void ApplyCongViecNoiBoFilter()
        {
            string keyword = SearchCongViecNoiBoTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
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

            // Tạo model thanh toán mới từ chi tiết nợ
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
                await AppProviders.ChiTietHoaDonNos.ReloadAsync();
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
                    AppProviders.ChiTietHoaDonNos.Remove(selected.Id);
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
        private void SearchChiTietHoaDonNoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyChiTietHoaDonNoFilter();
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
                // .Where(x => !x.IsDeleted && x.Ngay == now)
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
                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();
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
                    AppProviders.ChiTietHoaDonThanhToans.Remove(selected.Id);
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
        private void SearchChiTietHoaDonThanhToanTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyChiTietHoaDonThanhToanFilter();
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
                .Where(x => x.Ngay == now)
                .OrderByDescending(x => x.NgayGio)
                .ToList();
            ApplyChiTietHoaDonThanhToanFilter();
        }






        private List<ChiTieuHangNgayDto> _fullChiTieuHangNgayList = new();
        private async void AddChiTieuHangNgayButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ChiTieuHangNgayEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
            };
            if (window.ShowDialog() == true)
                await AppProviders.ChiTieuHangNgays.ReloadAsync();
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
                await AppProviders.ChiTieuHangNgays.ReloadAsync();
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
                    AppProviders.ChiTieuHangNgays.Remove(selected.Id);
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
        private readonly WpfErrorHandler _errorHandler = new();
        private void SearchChiTieuHangNgayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyChiTieuHangNgayFilter();
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
                .Where(x => x.Ngay == now)
                .OrderBy(x => x.BillThang)
                .ThenByDescending(x => x.NgayGio)
                .ToList();
            ApplyChiTieuHangNgayFilter();
        }





        private async Task LoadDoanhThuDynamic()
        {

            DoanhThuStackPanel.Children.Clear();

            await WaitForDataAsync(() => AppProviders.HoaDons?.Items != null);
            var groups = AppProviders.HoaDons.Items
                .Where(x => x.Ngay == now)
                .GroupBy(x => x.PhanLoai)
                .Select(g => new
                {
                    Loai = g.Key,
                    TongTien = g.Sum(x => x.ThanhTien)
                })
                .OrderBy(g => g.Loai)
                .OrderByDescending(g => g.TongTien)
                .ToList();

            // Tính tổng tất cả
            decimal tongTatCa = groups.Sum(g => g.TongTien);

            // Dòng tổng trên cùng (chữ to, đậm)
            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Doanh thu\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            DoanhThuStackPanel.Children.Add(totalTextBlock);

            // Các dòng chi tiết
            foreach (var group in groups)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var loaiText = new TextBlock { Text = $"{group.Loai}:" };
                var tienText = new TextBlock
                {
                    Text = $"{group.TongTien:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right
                };

                Grid.SetColumn(loaiText, 0);
                Grid.SetColumn(tienText, 1);

                grid.Children.Add(loaiText);
                grid.Children.Add(tienText);

                DoanhThuStackPanel.Children.Add(grid);
            }
        }
        private async void LoadDaThuDynamic()
        {
            DaThuStackPanel.Children.Clear();

            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonThanhToans?.Items != null);

            var groups = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => x.Ngay == now)
                .Where(x => x.LoaiThanhToan.ToLower().Contains("trong ngày"))
                .GroupBy(x => x.TenPhuongThucThanhToan) // Ví dụ: "Tiền mặt", "Chuyển khoản", ...
                .Select(g => new
                {
                    Loai = g.Key,
                    TongTien = g.Sum(x => x.SoTien)
                })
                .OrderByDescending(g => g.TongTien)
                .ToList();

            // Tính tổng tất cả
            decimal tongTatCa = groups.Sum(g => g.TongTien);

            // Dòng tổng trên cùng (chữ to, đậm, cn giữa)
            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Đã thu\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            DaThuStackPanel.Children.Add(totalTextBlock);

            // Các dòng chi tiết
            foreach (var group in groups)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var loaiText = new TextBlock
                {
                    Text = $"{group.Loai}:",
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var tienText = new TextBlock
                {
                    Text = $"{group.TongTien:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right
                };

                Grid.SetColumn(loaiText, 0);
                Grid.SetColumn(tienText, 1);

                grid.Children.Add(loaiText);
                grid.Children.Add(tienText);

                DaThuStackPanel.Children.Add(grid);
            }
        }
        private async void LoadChuaThuDynamic()
        {
            ChuaThuStackPanel.Children.Clear();
            // Lọc trước các hóa đơn còn nợ
            await WaitForDataAsync(() => AppProviders.HoaDons?.Items != null);

            var hoaDonChuaThu = AppProviders.HoaDons.Items
                .Where(x => x.Ngay == now)

                .Where(x => x.ConLai > 0 && x.TrangThai == "Chưa thu"
                )
                .OrderByDescending(g => g.TongTien)
                .ToList();


            // Tính tổng tất cả
            decimal tongTatCa = hoaDonChuaThu.Sum(g => g.ConLai);

            // Dòng tổng trên cùng (chữ to, đậm)
            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Chưa thu\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            ChuaThuStackPanel.Children.Add(totalTextBlock);

            // Các dòng chi tiết
            foreach (var hd in hoaDonChuaThu)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var loaiText = new TextBlock
                {
                    Text = $"{hd.TenKhachHangText}:",
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var tienText = new TextBlock
                {
                    Text = $"{hd.ConLai:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(loaiText, 0);
                Grid.SetColumn(tienText, 1);

                grid.Children.Add(loaiText);
                grid.Children.Add(tienText);

                ChuaThuStackPanel.Children.Add(grid);
            }
        }
        private async void LoadCongNoDynamic()
        {
            CongNoStackPanel.Children.Clear();

            // Gom nhóm theo KhachHangId
            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonNos?.Items != null);

            var groups = AppProviders.ChiTietHoaDonNos.Items
                .Where(x => x.Ngay == now)
                .GroupBy(x => new { x.KhachHangId, x.Ten })
                .Select(g => new
                {
                    KhachHangId = g.Key.KhachHangId,
                    TenKhachHang = g.Key.Ten,
                    TongTien = g.Sum(x => x.ConLai)
                })
                .OrderByDescending(g => g.TongTien)
                .ToList();

            // Tính tổng tất cả
            decimal tongTatCa = groups.Sum(g => g.TongTien);

            // Dòng tổng trên cùng (chữ to, đậm)
            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Ghi nợ\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            CongNoStackPanel.Children.Add(totalTextBlock);

            // Các dòng chi tiết
            foreach (var group in groups)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var loaiText = new TextBlock
                {
                    Text = $"{group.TenKhachHang}:",
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                var tienText = new TextBlock
                {
                    Text = $"{group.TongTien:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(loaiText, 0);
                Grid.SetColumn(tienText, 1);

                grid.Children.Add(loaiText);
                grid.Children.Add(tienText);

                CongNoStackPanel.Children.Add(grid);
            }
        }
        private async void LoadChiTieuDynamic()
        {
            ChiTieuStackPanel.Children.Clear();

            // Gom nhóm theo NguyenLieuId
            await WaitForDataAsync(() => AppProviders.ChiTieuHangNgays?.Items != null);

            var groups = AppProviders.ChiTieuHangNgays.Items
                .Where(x => x.Ngay == now)

                .Where(x => x.BillThang == false)
                .GroupBy(x => new { x.NguyenLieuId, x.Ten })
                .Select(g => new
                {
                    NguyenLieuId = g.Key.NguyenLieuId,
                    TenNguyenLieu = g.Key.Ten,
                    TongTien = g.Sum(x => x.ThanhTien)
                })
                .OrderByDescending(g => g.TongTien)
                .ToList();

            // Tính tổng chi tiêu hôm nay
            decimal tongTatCa = groups.Sum(g => g.TongTien);

            // Hiển thị dòng tổng trên cùng
            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Chi tiêu\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            ChiTieuStackPanel.Children.Add(totalTextBlock);

            // Hiển thị chi tiết từng nguyên liệu
            foreach (var group in groups)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var tenNguyenLieuText = new TextBlock
                {
                    Text = $"{group.TenNguyenLieu}:",
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                var soTienText = new TextBlock
                {
                    Text = $"{group.TongTien:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right
                };

                Grid.SetColumn(tenNguyenLieuText, 0);
                Grid.SetColumn(soTienText, 1);

                grid.Children.Add(tenNguyenLieuText);
                grid.Children.Add(soTienText);

                ChiTieuStackPanel.Children.Add(grid);
            }
        }
        private async void LoadMangVeDynamic()
        {
            MangVeStackPanel.Children.Clear();

            // Tính tổng tất cả
            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonThanhToans?.Items != null);

            decimal tongTienMat = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => x.Ngay == now)

                .Where(x => x.LoaiThanhToan.ToLower() == "trong ngày")
              .Where(x => x.TenPhuongThucThanhToan?.ToLower() == "tiền mặt")
              .Sum(x => x.SoTien);
            decimal tongChiTieu = AppProviders.ChiTieuHangNgays.Items
                .Where(x => x.Ngay == now)

                .Where(x => !x.BillThang)
                .Sum(x => x.ThanhTien);
            decimal tongChuaThu = AppProviders.HoaDons.Items
                .Where(x => x.Ngay == now)

                .Where(x => x.ConLai > 0 && x.TrangThai.ToLower() == "chưa thu")
               .Sum(g => g.ConLai);
            decimal tongTatCa = tongTienMat - tongChiTieu + tongChuaThu;

            decimal tongTraNo = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => x.Ngay == now)

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
        private async void LoadTraNoBankDynamic()
        {
            TraNoBankStackPanel.Children.Clear();

            // Nhóm theo KhachHangId
            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonThanhToans?.Items != null);

            var groups = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => x.Ngay == now)

                .Where(x => x.LoaiThanhToan.ToLower() == "trả nợ qua ngày")
                .Where(x => x.TenPhuongThucThanhToan?.ToLower() != "tiền mặt")
                .GroupBy(x => x.KhachHangId)
                .Select(g => new
                {
                    KhachHangId = g.Key,
                    Ten = g.FirstOrDefault()?.Ten ?? "Khách hàng lạ",
                    TongTien = g.Sum(x => x.SoTien)
                })
                .OrderByDescending(g => g.TongTien)
                .ToList();

            // Tổng tất cả
            decimal tongTatCa = groups.Sum(g => g.TongTien);

            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Trả nợ bank\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            TraNoBankStackPanel.Children.Add(totalTextBlock);

            foreach (var group in groups)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var loaiText = new TextBlock
                {
                    Text = $"{group.Ten}:",
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var tienText = new TextBlock
                {
                    Text = $"{group.TongTien:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right
                };

                Grid.SetColumn(loaiText, 0);
                Grid.SetColumn(tienText, 1);

                grid.Children.Add(loaiText);
                grid.Children.Add(tienText);

                TraNoBankStackPanel.Children.Add(grid);
            }
        }
        private async void LoadTraNoTienDynamic()
        {
            TraNoTienStackPanel.Children.Clear();
            await WaitForDataAsync(() => AppProviders.ChiTietHoaDonThanhToans?.Items != null);

            var groups = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => x.Ngay == now)

                .Where(x => x.LoaiThanhToan.ToLower() == "trả nợ qua ngày")
                .Where(x => x.TenPhuongThucThanhToan?.ToLower() == "tiền mặt")
                .GroupBy(x => x.KhachHangId)
                .Select(g => new
                {
                    KhachHangId = g.Key,
                    Ten = g.FirstOrDefault()?.Ten ?? "Khách hàng lạ",
                    TongTien = g.Sum(x => x.SoTien)
                })
                .OrderByDescending(g => g.TongTien)
                .ToList();

            decimal tongTatCa = groups.Sum(g => g.TongTien);

            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Trả nợ tiền\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            TraNoTienStackPanel.Children.Add(totalTextBlock);

            foreach (var group in groups)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var loaiText = new TextBlock
                {
                    Text = $"{group.Ten}:",
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var tienText = new TextBlock
                {
                    Text = $"{group.TongTien:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right
                };

                Grid.SetColumn(loaiText, 0);
                Grid.SetColumn(tienText, 1);

                grid.Children.Add(loaiText);
                grid.Children.Add(tienText);

                TraNoTienStackPanel.Children.Add(grid);
            }
        }
        private async Task UpdateDashboardSummary()
        {
            try
            {
                // Load các panel
                LoadDoanhThuDynamic();
                LoadChuaThuDynamic();
                LoadDaThuDynamic();
                LoadTraNoTienDynamic();
                LoadTraNoBankDynamic();
                LoadChiTieuDynamic();
                LoadMangVeDynamic();
                LoadCongNoDynamic();

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
                var result = await ApiClient.GetAsync("/api/dashboard/homnay");
                var dashboard = await result.Content.ReadFromJsonAsync<DashboardDto>();
                if (dashboard != null)
                {
                    BanNhieuGrid.ItemsSource = dashboard.TopSanPhams ?? new List<DashboardTopSanPhamDto>();
                }

                var result2 = await ApiClient.GetAsync("/api/dashboard/dubao");
                var dashboard2 = await result2.Content.ReadFromJsonAsync<DashboardDto>();
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
        private async void AddHoaDonButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new HoaDonEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
            };
            if (window.ShowDialog() == true)
                await AppProviders.HoaDons.ReloadAsync();
        }

        private async void HoaDonDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
                return;

            var api = new HoaDonApi();

            // Lấy chi tiết hóa đơn
            var getResult = await api.GetByIdAsync(selected.Id);
            if (!getResult.IsSuccess)
            {
                MessageBox.Show($"Lỗi: {getResult.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                ChiTietHoaDonListBox.ItemsSource = null;
                return;
            }

            ChiTietHoaDonListBox.ItemsSource = getResult.Data.ChiTietHoaDons;

            // Nếu hóa đơn đang được báo đơn => tắt báo đơn
            if (selected.BaoDon == true)
            {
                selected.BaoDon = false;
                var updateResult = await api.UpdateSingleAsync(selected.Id, selected);

                if (!updateResult.IsSuccess)
                {
                    MessageBox.Show($"Lỗi: {updateResult.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    await AppProviders.HoaDons.ReloadAsync();
                    ReloadHoaDonUI();
                }
            }
        }
        private async void HoaDonDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;
            var window = new HoaDonEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this
            };
            if (window.ShowDialog() == true)
                await AppProviders.HoaDons.ReloadAsync();
        }
        private async void XoaHoaDonButton_Click(object sender, RoutedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
                return;
            var confirm = MessageBox.Show(
               $"Bạn có chắc chắn muốn xoá '{selected.Ten}'?",
               "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/HoaDon/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<HoaDonDto>>();

                if (result?.IsSuccess == true)
                {
                    AppProviders.HoaDons.Remove(selected.Id);
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
        private void SearchHoaDonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyHoaDonFilter();
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
                .Where(x => x.Ngay == now
                || x.TrangThai == "Chưa thu"
                || x.TrangThai == "Thu một phần")
                .OrderByDescending(x => x.NgayGio)
                .ToList();
            ApplyHoaDonFilter();
        }





        string oldConn = "Server=192.168.1.85;Database=DennCoffee;user=sa;password=baothanh1991;TrustServerCertificate=True";
        string newConn = "Server=.;Database=TraSuaAppDb;Trusted_Connection=True;TrustServerCertificate=True";
        private DateTime now;
        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var importer = new KhachHangImporter(oldConn, newConn);
            await importer.ImportAsync();

            var importer2 = new HoaDonImporter(oldConn, newConn);
            await importer2.ImportTodayAsync();
        }

        //private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (e.Source is TabControl tabControl)
        //    {
        //        var selectedTab = tabControl.SelectedItem as TabItem;
        //        if (selectedTab != null)
        //        {
        //            if (selectedTab.Tag.ToString() == "ThongKeHomNay")
        //            {
        //                var result = await ApiClient.GetAsync("/api/dashboard/homnay");
        //                var dashboard = await result.Content.ReadFromJsonAsync<DashboardDto>();
        //                if (dashboard == null) return;
        //                BanNhieuGrid.ItemsSource = dashboard.TopSanPhams;


        //                var result2 = await ApiClient.GetAsync("/api/dashboard/dubao");
        //                var dashboard2 = await result2.Content.ReadFromJsonAsync<DashboardDto>();
        //                if (dashboard2 == null) return;
        //                DuDoanDoanhThu.Text = dashboard2.PredictedPeak;

        //            }
        //            else
        //            if (selectedTab.Tag.ToString() == "HoaDon")
        //            {
        //                await WaitForDataAsync(() => AppProviders.HoaDons?.Items != null);

        //                await AppProviders.HoaDons.ReloadAsync();
        //                ReloadHoaDonUI();
        //            }
        //            else if (selectedTab.Tag.ToString() == "ChiTieuHangNgay")
        //            {
        //                await WaitForDataAsync(() => AppProviders.ChiTieuHangNgays?.Items != null);

        //                await AppProviders.ChiTieuHangNgays.ReloadAsync();
        //                ReloadChiTieuHangNgayUI();
        //            }
        //            else if (selectedTab.Tag.ToString() == "ChiTietHoaDonNo")
        //            {
        //                await WaitForDataAsync(() => AppProviders.ChiTietHoaDonNos?.Items != null);
        //                await AppProviders.ChiTietHoaDonNos.ReloadAsync();
        //                ReloadChiTietHoaDonNoUI();
        //            }
        //            else if (selectedTab.Tag.ToString() == "CongViecNoiBo")
        //            {
        //                await WaitForDataAsync(() => AppProviders.CongViecNoiBos?.Items != null);
        //                await AppProviders.CongViecNoiBos.ReloadAsync();
        //                ReloadCongViecNoiBoUI();
        //            }
        //            else if (selectedTab.Tag.ToString() == "ChiTietHoaDonThanhToan")
        //            {
        //                await WaitForDataAsync(() => AppProviders.ChiTietHoaDonThanhToans?.Items != null);
        //                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();
        //                ReloadChiTietHoaDonThanhToanUI();
        //            }

        //        }
        //    }



        //}
        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is not TabControl tabControl) return;

            var selectedTab = tabControl.SelectedItem as TabItem;
            if (selectedTab == null) return;

            var tag = selectedTab.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            // Dictionary map tag → async action
            var loadActions = new Dictionary<string, Func<Task>>
            {
                ["ThongKeHomNay"] = async () =>
                {
                    // Load top sản phẩm hôm nay
                    var result1 = await ApiClient.GetAsync("/api/dashboard/homnay");
                    if (result1.IsSuccessStatusCode)
                    {
                        var json1 = await result1.Content.ReadAsStringAsync();
                        var dashboard1 = JsonSerializer.Deserialize<DashboardDto>(json1,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (dashboard1 != null)
                            BanNhieuGrid.ItemsSource = dashboard1.TopSanPhams;
                    }

                    // Load dự đoán doanh thu
                    var result2 = await ApiClient.GetAsync("/api/dashboard/dubao");
                    if (result2.IsSuccessStatusCode)
                    {
                        var json2 = await result2.Content.ReadAsStringAsync();
                        var dashboard2 = JsonSerializer.Deserialize<DashboardDto>(json2,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (dashboard2 != null)
                            DuDoanDoanhThu.Text = dashboard2.PredictedPeak;
                    }
                },

                ["HoaDon"] = async () =>
                {
                    var provider = AppProviders.HoaDons;
                    await this.BindProviderAsync(
                        waitCondition: () => provider.Items != null,
                        subscribe: act => { },
                        reloadAction: ReloadHoaDonUI,
                        name: "HoaDon"
                    );
                },

                ["ChiTieuHangNgay"] = async () =>
                {
                    var provider = AppProviders.ChiTieuHangNgays;
                    await this.BindProviderAsync(
                        waitCondition: () => provider.Items != null,
                        subscribe: act => { },
                        reloadAction: ReloadChiTieuHangNgayUI,
                        name: "ChiTieuHangNgay"
                    );
                },

                ["ChiTietHoaDonNo"] = async () =>
                {
                    var provider = AppProviders.ChiTietHoaDonNos;
                    await this.BindProviderAsync(
                        waitCondition: () => provider.Items != null,
                        subscribe: act => { },
                        reloadAction: ReloadChiTietHoaDonNoUI,
                        name: "ChiTietHoaDonNo"
                    );
                },

                ["CongViecNoiBo"] = async () =>
                {
                    var provider = AppProviders.CongViecNoiBos;
                    await this.BindProviderAsync(
                        waitCondition: () => provider.Items != null,
                        subscribe: act => { },
                        reloadAction: ReloadCongViecNoiBoUI,
                        name: "CongViecNoiBo"
                    );
                },

                ["ChiTietHoaDonThanhToan"] = async () =>
                {
                    var provider = AppProviders.ChiTietHoaDonThanhToans;
                    await this.BindProviderAsync(
                        waitCondition: () => provider.Items != null,
                        subscribe: act => { },
                        reloadAction: ReloadChiTietHoaDonThanhToanUI,
                        name: "ChiTietHoaDonThanhToan"
                    );
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
                            EscButton_Click(null, null);
                            break;
                        case Key.F1:
                            F1Button_Click(null, null);
                            break;
                        case Key.F2:
                            F2Button_Click(null, null);
                            break;
                        case Key.F3:
                            F3Button_Click(null, null);
                            break;
                        case Key.F4:
                            F4Button_Click(null, null);
                            break;
                        case Key.F5:
                            F5Button_Click(null, null);
                            break;
                        case Key.F6:
                            F6Button_Click(null, null);
                            break;
                        case Key.F7:
                            F7Button_Click(null, null);
                            break;
                        case Key.F8:
                            F8Button_Click(null, null);
                            break;
                        case Key.F9:
                            F9Button_Click(null, null);
                            break;
                        case Key.F12:
                            F12Button_Click(null, null);
                            break;
                        case Key.Delete:
                            DelButton_Click(null, null);
                            break;
                    }
                }
                else
                if (selectedTab.Tag.ToString() == "ChiTietHoaDonNo")
                {
                    switch (e.Key)
                    {
                        case Key.F1:
                            F1aButton_Click(null, null);
                            break;
                        case Key.F4:
                            F4aButton_Click(null, null);
                            break;
                        case Key.F5:
                            F5aButton_Click(null, null);
                            break;
                    }
                }
            }
        }
        private async void EscButton_Click(object sender, RoutedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
            {
                MessageBox.Show("Vui lòng chọn hoá đơn!");
                return;
            }
            EscButton.IsEnabled = false;

            selected.NgayShip = DateTime.Now;

            var api = new HoaDonApi();
            var result = await api.UpdateSingleAsync(selected.Id, selected);

            if (!result.IsSuccess)
            {
                MessageBox.Show($"Lỗi: {result.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                await AppProviders.HoaDons.ReloadAsync();
                ReloadHoaDonUI();
            }

            await Task.Delay(1000);
            EscButton.IsEnabled = true;
        }
        private async void F1Button_Click(object sender, RoutedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
            {
                MessageBox.Show("Vui lòng chọn hoá đơn!");
                return;
            }
            if (selected.ConLai == 0)
            {
                MessageBox.Show("Hoá đơn đã thu đủ!");
                return;
            }
            if (selected.TrangThai.ToLower().Contains("nợ"))
            {
                MessageBox.Show("Vui lòng thanh toán tại tab Công nợ!");
                return;
            }

            F1Button.IsEnabled = false;

            var now = DateTime.Now;
            var trongngay = now.Date == selected.Ngay;
            var dto = new ChiTietHoaDonThanhToanDto
            {
                Ngay = trongngay ? now.Date : selected.Ngay,
                NgayGio = trongngay ? now : selected.Ngay.AddDays(1).AddMinutes(-1),
                HoaDonId = selected.Id,
                KhachHangId = selected.KhachHangId,
                Ten = $"{selected.Ten}",
                LoaiThanhToan = "Trong ngày",
                PhuongThucThanhToanId = Guid.Parse("0121FC04-0469-4908-8B9A-7002F860FB5C"),
                SoTien = selected.ConLai,
            };

            var window = new ChiTietHoaDonThanhToanEdit(dto)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
                //0Background = MakeBrush(Brushes.LightGreen, 0.8)
            };
            window.PhuongThucThanhToanComboBox.IsEnabled = false;

            if (window.ShowDialog() == true)
            {
                await AppProviders.HoaDons.ReloadAsync();
                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();

            }


            await Task.Delay(100);
            F1Button.IsEnabled = true;
        }
        private async void F2Button_Click(object sender, RoutedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
            {
                MessageBox.Show("Vui lòng chọn hoá đơn!");
                return;
            }
            F2Button.IsEnabled = false;

            try
            {
                // 🟟 Lấy lại hóa đơn đầy đủ từ DB (bao gồm ChiTietHoaDons, Topping, Voucher...)
                var api = new HoaDonApi();
                var result = await api.GetByIdAsync(selected.Id);

                if (!result.IsSuccess || result.Data == null)
                {
                    MessageBox.Show($"Không thể tải chi tiết hóa đơn: {result.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                HoaDonPrinter.Print(result.Data);
                //HoaDonPrinter.Preview(result.Data, this);
            }
            finally
            {
                await Task.Delay(100);
                F2Button.IsEnabled = true;
            }
        }
        private async void F3Button_Click(object sender, RoutedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
            {
                MessageBox.Show("Vui lòng chọn hoá đơn!");
                return;
            }

            F3Button.IsEnabled = false;

            try
            {
                // 🟟 Lấy lại hóa đơn đầy đủ từ DB
                var api = new HoaDonApi();
                var result = await api.GetByIdAsync(selected.Id);

                if (!result.IsSuccess || result.Data == null)
                {
                    MessageBox.Show($"Không thể tải chi tiết hóa đơn: {result.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 🟟 Chỉ copy vào clipboard, không show, không in
                HoaDonPrinter.Copy(result.Data);
                MessageBox.Show("Đã copy, ctrl v để gửi!");

            }
            finally
            {
                await Task.Delay(100);
                F3Button.IsEnabled = true;
            }
        }
        private async void F4Button_Click(object sender, RoutedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
            {
                MessageBox.Show("Vui lòng chọn hoá đơn!");
                return;
            }
            if (selected.ConLai == 0)
            {
                MessageBox.Show("Hoá đơn đã thu đủ!");
                return;
            }
            if (selected.TrangThai.ToLower().Contains("nợ"))
            {
                MessageBox.Show("Vui lòng thanh toán tại tab Công nợ!");
                return;
            }

            F4Button.IsEnabled = false;

            var now = DateTime.Now;
            var trongngay = now.Date == selected.Ngay;
            var dto = new ChiTietHoaDonThanhToanDto
            {
                Ngay = trongngay ? now.Date : selected.Ngay,
                NgayGio = trongngay ? now : selected.Ngay.AddDays(1).AddMinutes(-1),
                HoaDonId = selected.Id,
                KhachHangId = selected.KhachHangId,
                Ten = $"{selected.Ten}",
                LoaiThanhToan = "Trong ngày",
                PhuongThucThanhToanId = Guid.Parse("2cf9a88f-3bc0-4d4b-940d-f8ffa4affa02"),
                SoTien = selected.ConLai,
            };

            var window = new ChiTietHoaDonThanhToanEdit(dto)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
                // Background = MakeBrush(Brushes.LightYellow, 0.8)

            };
            window.PhuongThucThanhToanComboBox.IsEnabled = false;

            if (window.ShowDialog() == true)
            {
                await AppProviders.HoaDons.ReloadAsync();
                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();

            }


            await Task.Delay(100);
            F4Button.IsEnabled = true;
        }
        private async void F5Button_Click(object sender, RoutedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
            {
                MessageBox.Show("Vui lòng chọn hoá đơn!");
                return;
            }
            if (selected.ConLai == 0)
            {
                MessageBox.Show("Hoá đơn đã thu đủ!");
                return;
            }
            if (selected.TrangThai.ToLower().Contains("nợ"))
            {
                MessageBox.Show("Vui lòng thanh toán tại tab Công nợ!");
                return;
            }

            F5Button.IsEnabled = false;

            var now = DateTime.Now;
            var trongngay = now.Date == selected.Ngay;
            var dto = new ChiTietHoaDonThanhToanDto
            {
                Ngay = trongngay ? now.Date : selected.Ngay,
                NgayGio = trongngay ? now : selected.Ngay.AddDays(1).AddMinutes(-1),
                HoaDonId = selected.Id,
                KhachHangId = selected.KhachHangId,
                Ten = $"{selected.Ten}",
                LoaiThanhToan = "Trong ngày",
                PhuongThucThanhToanId = Guid.Parse("3d75dd9f-a5d3-491d-a316-6d5c9ff7e66c"),
                SoTien = selected.ConLai,
            };

            var window = new ChiTietHoaDonThanhToanEdit(dto)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
                // Background = MakeBrush(Brushes.Gold, 0.8)
            };
            window.PhuongThucThanhToanComboBox.IsEnabled = false;
            if (window.ShowDialog() == true)
            {
                await AppProviders.HoaDons.ReloadAsync();
                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();

            }


            await Task.Delay(100);
            F5Button.IsEnabled = true;
        }
        private async void F6Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Images/viettin007.jpg"); // đổi tên file
                BitmapImage bitmap = new BitmapImage(uri);
                Clipboard.SetImage(bitmap);
                MessageBox.Show("Đã copy, ctrl v để gửi!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");
            }


        }
        private async void F7Button_Click(object sender, RoutedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
            {
                MessageBox.Show("Vui lòng chọn hoá đơn!");
                return;
            }
            F7Button.IsEnabled = false;
            if (selected.BaoDon != true)
                selected.BaoDon = true;
            else
                selected.BaoDon = false;


            var api = new HoaDonApi();
            var result = await api.UpdateSingleAsync(selected.Id, selected);

            if (!result.IsSuccess)
            {
                MessageBox.Show($"Lỗi: {result.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                await AppProviders.HoaDons.ReloadAsync();
                ReloadHoaDonUI();
            }
            await Task.Delay(1000);
            F7Button.IsEnabled = true;
        }
        private async void F8Button_Click(object sender, RoutedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
            {
                MessageBox.Show("Vui lòng chọn hoá đơn!");
                return;
            }
            F8Button.IsEnabled = false;
            if (selected.UuTien != true)
                selected.UuTien = true;
            else
                selected.UuTien = false;


            var api = new HoaDonApi();
            var result = await api.UpdateSingleAsync(selected.Id, selected);

            if (!result.IsSuccess)
            {
                MessageBox.Show($"Lỗi: {result.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                await AppProviders.HoaDons.ReloadAsync();
                ReloadHoaDonUI();
            }
            await Task.Delay(1000);
            F8Button.IsEnabled = true;
        }
        private async void F9Button_Click(object sender, RoutedEventArgs e)
        {
            if (HenGioStackPanel.Visibility != Visibility.Visible)
            {
                GioCombo.SelectedIndex = DateTime.Now.Hour - 6;
                PhutCombo.SelectedIndex = DateTime.Now.Minute / 10;

                HenGioStackPanel.Visibility = Visibility.Visible;
            }
            else
                HenGioStackPanel.Visibility = Visibility.Collapsed;



        }
        private async void OkHenGioButton_Click(object sender, RoutedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
            {
                MessageBox.Show("Vui lòng chọn hoá đơn!");
                return;
            }
            OkHenGio.IsEnabled = false;

            int.TryParse(GioCombo.SelectedItem?.ToString(), out int gio);
            int.TryParse(PhutCombo.SelectedItem?.ToString(), out int phut);

            selected.NgayHen = DateTime.Now.Date.AddHours(gio).AddMinutes(phut);

            var api = new HoaDonApi();
            var result = await api.UpdateSingleAsync(selected.Id, selected);

            if (!result.IsSuccess)
            {
                MessageBox.Show($"Lỗi: {result.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                await AppProviders.HoaDons.ReloadAsync();
                ReloadHoaDonUI();
                HenGioStackPanel.Visibility = Visibility.Collapsed;

            }
            await Task.Delay(1000);
            OkHenGio.IsEnabled = true;
        }
        private async void F12Button_Click(object sender, RoutedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
            {
                MessageBox.Show("Vui lòng chọn hoá đơn!");
                return;
            }
            if (selected.ConLai == 0)
            {
                MessageBox.Show("Hoá đơn đã thu đủ!");
                return;
            }
            if (selected.TrangThai.ToLower().Contains("nợ"))
            {
                MessageBox.Show("Hoá đơn đã ghi nợ!");
                return;
            }
            if (selected.KhachHangId == null)
            {
                MessageBox.Show("Hoá đơn chưa có thông tin khách hàng!");
                return;
            }

            F12Button.IsEnabled = false;

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
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
                Background = MakeBrush(Brushes.IndianRed, 0.8),
            };
            window.SoTienTextBox.IsReadOnly = true;
            if (window.ShowDialog() == true)
            {
                await AppProviders.HoaDons.ReloadAsync();
                await AppProviders.ChiTietHoaDonNos.ReloadAsync();
            }

            await Task.Delay(100);
            F12Button.IsEnabled = true;

        }
        public static SolidColorBrush MakeBrush(SolidColorBrush brush, double opacity = 1.0)
        {
            var color = brush.Color;
            var newBrush = new SolidColorBrush(color);
            newBrush.Opacity = opacity; // 0.0 -> 1.0
            return newBrush;
        }
        private async void F1aButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected)
            {
                MessageBox.Show("Vui lòng chọn công nợ!");
                return;
            }
            if (selected.ConLai == 0)
            {
                MessageBox.Show("Công nợ đã thu đủ!");
                return;
            }

            //  F1Button.IsEnabled = false;

            var now = DateTime.Now;
            var trongngay = now.Date == selected.Ngay;
            var dto = new ChiTietHoaDonThanhToanDto
            {
                ChiTietHoaDonNoId = selected.Id,
                Ngay = now.Date,
                NgayGio = now,
                HoaDonId = selected.HoaDonId,
                KhachHangId = selected.KhachHangId,
                Ten = $"{selected.Ten}",
                PhuongThucThanhToanId = Guid.Parse("0121FC04-0469-4908-8B9A-7002F860FB5C"),
                LoaiThanhToan = selected.Ngay == now.Date ? "Trả nợ trong ngày" : "Trả nợ qua ngày",
                GhiChu = selected.GhiChu,
                SoTien = selected.ConLai,
            };

            var window = new ChiTietHoaDonThanhToanEdit(dto)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
                //0Background = MakeBrush(Brushes.LightGreen, 0.8)
            };
            window.PhuongThucThanhToanComboBox.IsEnabled = false;

            if (window.ShowDialog() == true)
            {
                await AppProviders.ChiTietHoaDonNos.ReloadAsync();
                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();

            }


            await Task.Delay(100);
            // F1Button.IsEnabled = true;
        }
        private async void F4aButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected)
            {
                MessageBox.Show("Vui lòng chọn công nợ!");
                return;
            }
            if (selected.ConLai == 0)
            {
                MessageBox.Show("Công nợ đã thu đủ!");
                return;
            }

            //  F1Button.IsEnabled = false;

            var now = DateTime.Now;
            var trongngay = now.Date == selected.Ngay;
            var dto = new ChiTietHoaDonThanhToanDto
            {
                PhuongThucThanhToanId = Guid.Parse("2cf9a88f-3bc0-4d4b-940d-f8ffa4affa02"),

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
                Owner = this,
                //0Background = MakeBrush(Brushes.LightGreen, 0.8)
            };
            window.PhuongThucThanhToanComboBox.IsEnabled = false;

            if (window.ShowDialog() == true)
            {
                await AppProviders.ChiTietHoaDonNos.ReloadAsync();
                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();

            }


            await Task.Delay(100);
            // F1Button.IsEnabled = true;
        }
        private async void F5aButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected)
            {
                MessageBox.Show("Vui lòng chọn công nợ!");
                return;
            }
            if (selected.ConLai == 0)
            {
                MessageBox.Show("Công nợ đã thu đủ!");
                return;
            }

            //  F1Button.IsEnabled = false;

            var now = DateTime.Now;
            var trongngay = now.Date == selected.Ngay;
            var dto = new ChiTietHoaDonThanhToanDto
            {
                ChiTietHoaDonNoId = selected.Id,
                Ngay = now.Date,
                NgayGio = now,
                HoaDonId = selected.HoaDonId,
                KhachHangId = selected.KhachHangId,
                Ten = $"{selected.Ten}",
                PhuongThucThanhToanId = Guid.Parse("3d75dd9f-a5d3-491d-a316-6d5c9ff7e66c"),

                LoaiThanhToan = selected.Ngay == now.Date ? "Trả nợ trong ngày" : "Trả nợ qua ngày",
                GhiChu = selected.GhiChu,
                SoTien = selected.ConLai,
            };

            var window = new ChiTietHoaDonThanhToanEdit(dto)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
                //0Background = MakeBrush(Brushes.LightGreen, 0.8)
            };
            window.PhuongThucThanhToanComboBox.IsEnabled = false;

            if (window.ShowDialog() == true)
            {
                await AppProviders.ChiTietHoaDonNos.ReloadAsync();
                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();

            }


            await Task.Delay(100);
            // F1Button.IsEnabled = true;
        }
        private async void DelButton_Click(object sender, RoutedEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected)
                return;
            var confirm = MessageBox.Show(
               $"Bạn có chắc chắn muốn xoá '{selected.Ten}'?",
               "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/HoaDon/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<HoaDonDto>>();

                if (result?.IsSuccess == true)
                {
                    AppProviders.HoaDons.Remove(selected.Id);
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

        private async void AppButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppButton.IsEnabled = false;

                var helper = new AppShippingHelperText("12122431577", "baothanh1991");

                // Chạy trên background thread
                var hoaDon = await Task.Run(() => helper.GetFirstOrderPopup());

                var now = DateTime.Now;
                var dto = new HoaDonDto
                {
                    Ngay = now.Date,
                    NgayGio = now,
                    MaHoaDon = hoaDon.Code,
                    // KhachHangId = selected.KhachHangId,
                };

                var window = new HoaDonEdit(dto)
                {
                    Width = this.ActualWidth,
                    Height = this.ActualHeight,
                    Owner = this,
                };

                if (window.ShowDialog() == true)
                {
                    await AppProviders.HoaDons.ReloadAsync();
                }


                await Task.Delay(100);
                AppButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                NotiHelper.Show(ex.Message);
            }
        }
    }
}

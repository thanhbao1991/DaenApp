using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private MediaPlayer _baoDonPlayer;

        // 🟟 Timer tách riêng
        private readonly DispatcherTimer _baoDonTimer;
        private readonly DispatcherTimer _congViecTimer;
        private readonly DispatcherTimer _updateSummaryTimer;

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
            _baoDonPlayer = new MediaPlayer();
            var uri = new Uri("pack://application:,,,/Resources/ring3.wav");
            _baoDonPlayer.Open(uri);
            _baoDonPlayer.Volume = 0.8;
            _baoDonPlayer.MediaEnded += (s, e) =>
            {
                _baoDonPlayer.Position = TimeSpan.Zero;
                _baoDonPlayer.Play();
            };

            // 🟟 Timer báo đơn (2s)
            _baoDonTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
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

            // 🟟 Timer update summary (debounce 0.5s)
            _updateSummaryTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _updateSummaryTimer.Tick += async (s, e) =>
            {
                _updateSummaryTimer.Stop();
                await UpdateDashboardSummary();
            };

            Loaded += Dashboard_Loaded;

            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            GenerateMenu("Admin", AdminMenu);
            GenerateMenu("HoaDon", HoaDonMenu);
            GenerateMenu("Settings", SettingsMenu);
            for (int h = 6; h < 22; h++) GioCombo.Items.Add(h.ToString("D2"));
            for (int m = 0; m < 60; m += 10) PhutCombo.Items.Add(m.ToString("D2"));
        }

        // 🟟 Timer báo đơn
        private async Task BaoDonTimer_Tick()
        {
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

            if (DateTime.Now.Second < 2)
            {
                foreach (var item in _fullHoaDonList)
                    item.RefreshGioHienThi();
            }

            var dsDenHan = _fullHoaDonList
                .Where(h => h.NgayHen.HasValue && h.NgayHen.Value <= DateTime.Now)
                .ToList();

            foreach (var hd in dsDenHan)
            {
                NotiHelper.Show($"⏰ Đến giờ hẹn: {hd.Ten} ({hd.TongTien:N0}đ)");

                hd.NgayHen = null;
                var api = new HoaDonApi();
                await api.UpdateSingleAsync(hd.Id, hd);
            }
        }

        // 🟟 Timer công việc
        private async Task CongViecTimer_Tick()
        {
            if (AppProviders.CongViecNoiBos != null)
            {
                var congViec = _fullCongViecNoiBoList
                    .Where(cv => !cv.DaHoanThanh)
                    .OrderBy(x => x.NgayGio)
                    .FirstOrDefault();

                if (congViec != null)
                    await TTSHelper.DownloadAndPlayGoogleTTSAsync(congViec.Ten);
            }
        }

        private void ScheduleUpdateDashboardSummary()
        {
            _updateSummaryTimer.Stop();
            _updateSummaryTimer.Start();
        }

        private void SearchCongViecNoiBoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debouncer.Debounce("CongViecNoiBo", 300, ApplyCongViecNoiBoFilter);
        }

        private void SearchChiTietHoaDonNoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debouncer.Debounce("ChiTietNo", 300, ApplyChiTietHoaDonNoFilter);
        }

        private void SearchChiTietHoaDonThanhToanTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debouncer.Debounce("ThanhToan", 300, ApplyChiTietHoaDonThanhToanFilter);
        }

        private void SearchChiTieuHangNgayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debouncer.Debounce("ChiTieu", 300, ApplyChiTieuHangNgayFilter);
        }

        private void SearchHoaDonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debouncer.Debounce("HoaDon", 300, ApplyHoaDonFilter);

            if (sender is TextBox tb)
            {
                // đếm số dòng hiện tại trong TextBox
                int lineCount = tb.LineCount;
                if (lineCount < 1) lineCount = 1;

                // mỗi dòng cao khoảng 32px
                tb.Height = 32 * lineCount;
            }
        }
        private void SearchChiTietHoaDonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debouncer.Debounce("ChiTietHoaDon", 300, () =>
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

                int stt = 1;
                foreach (var item in sourceList)
                {
                    item.Stt = stt++;
                }

                ChiTietHoaDonListBox.ItemsSource = sourceList;
                tongTien = sourceList.Sum(x => x.ThanhTien);
            });
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
                today = DateTime.Today.AddDays(0);

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

        private List<ChiTietHoaDonDto> _fullChiTietHoaDonList = new();
        private async Task BindAllProviders()
        {
            var providers = new List<(Func<bool> wait, Action<Action> subscribe, Action reload, string name)>
    {
        (
            () => AppProviders.HoaDons?.Items != null,
            h =>
            {
                if (AppProviders.HoaDons != null)
                {
                    AppProviders.HoaDons.OnChanged -= h; // tránh trùng event
                    AppProviders.HoaDons.OnChanged += h;
                }
            },
            ReloadHoaDonUI,
            "HoaDons"
        ),

        (
            () => AppProviders.CongViecNoiBos?.Items != null,
            h =>
            {
                if (AppProviders.CongViecNoiBos != null)
                {
                    AppProviders.CongViecNoiBos.OnChanged -= h;
                    AppProviders.CongViecNoiBos.OnChanged += h;
                }
            },
            ReloadCongViecNoiBoUI,
            "CongViecNoiBos"
        ),

        (
            () => AppProviders.ChiTietHoaDonNos?.Items != null,
            h =>
            {
                if (AppProviders.ChiTietHoaDonNos != null)
                {
                    AppProviders.ChiTietHoaDonNos.OnChanged -= h;
                    AppProviders.ChiTietHoaDonNos.OnChanged += h;
                }
            },
            ReloadChiTietHoaDonNoUI,
            "ChiTietHoaDonNos"
        ),

        (
            () => AppProviders.ChiTietHoaDonThanhToans?.Items != null,
            h =>
            {
                if (AppProviders.ChiTietHoaDonThanhToans != null)
                {
                    AppProviders.ChiTietHoaDonThanhToans.OnChanged -= h;
                    AppProviders.ChiTietHoaDonThanhToans.OnChanged += h;
                }
            },
            ReloadChiTietHoaDonThanhToanUI,
            "ChiTietHoaDonThanhToans"
        ),

        (
            () => AppProviders.ChiTieuHangNgays?.Items != null,
            h =>
            {
                if (AppProviders.ChiTieuHangNgays != null)
                {
                    AppProviders.ChiTieuHangNgays.OnChanged -= h;
                    AppProviders.ChiTieuHangNgays.OnChanged += h;
                }
            },
            ReloadChiTieuHangNgayUI,
            "ChiTieuHangNgays"
        )
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
            tongTien = sourceList.Sum(x => x.ConLai);

            TongTienChiTietHoaDonNoTextBlock.Header = $"{tongTien:N0} đ";



        }
        private void ReloadChiTietHoaDonNoUI()
        {
            _fullChiTietHoaDonNoList = AppProviders.ChiTietHoaDonNos.Items
                .Where(x => x.ConLai > 0 || x.Ngay == today)
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
        private void ReloadChiTieuHangNgayUI()
        {
            _fullChiTieuHangNgayList = AppProviders.ChiTieuHangNgays.Items
                  .Where(x => !x.IsDeleted)
                 .Where(x => x.Ngay == today)
                // .OrderBy(x => x.BillThang)
                .OrderByDescending(x => x.NgayGio)
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

        private SanPhamBienTheDto? ChonBienTheFallback(SanPhamDto sp, string? bienTheTenTuLLM)
        {
            // Ưu tiên tên biến thể LLM trả về
            var match = sp.BienThe.FirstOrDefault(bt =>
                bt.TenBienThe.Equals(bienTheTenTuLLM ?? "", StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;

            // Nếu chỉ có 1 biến thể thì lấy luôn
            if (sp.BienThe.Count == 1) return sp.BienThe[0];

            // Mặc định (được cấu hình)
            var macDinh = sp.BienThe.FirstOrDefault(bt => bt.MacDinh);
            if (macDinh != null) return macDinh;

            // Size chuẩn nếu có
            var sizeChuan = sp.BienThe.FirstOrDefault(bt =>
                bt.TenBienThe.Equals("Size chuẩn", StringComparison.OrdinalIgnoreCase));
            if (sizeChuan != null) return sizeChuan;

            // Cuối cùng: first
            return sp.BienThe.FirstOrDefault();
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
                .Where(x => x.ConLai > 0 && (x.TrangThai == "Chưa thu" || x.TrangThai == "Thu một phần"))
                .OrderByDescending(x => x.NgayGio)
                .Select(hd => (Label: hd.KhachHangId != null ? hd.TenKhachHangText : hd.TenBan, Value: hd.ConLai))
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
                .Where(x => x.LoaiThanhToan == "Trả nợ qua ngày")
                //   .Where(x => x.GhiChu != "Shipper")
                .Where(x => x.TenPhuongThucThanhToan == "Tiền mặt")
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
                .Where(x => x.TenPhuongThucThanhToan == "Tiền mặt")
                .Where(x => x.LoaiThanhToan == "Trong ngày" || x.LoaiThanhToan == "Trả nợ trong ngày")
                .Where(x => x.GhiChu != "Shipper")
                .Sum(x => x.SoTien);
            decimal tongChiTieu = AppProviders.ChiTieuHangNgays.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => !x.BillThang)
                .Sum(x => x.ThanhTien);
            decimal tongKhanh = AppProviders.ChiTietHoaDonThanhToans.Items
              .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => x.TenPhuongThucThanhToan == "Tiền mặt")
                .Where(x => x.LoaiThanhToan == "Trong ngày" || x.LoaiThanhToan == "Trả nợ trong ngày")
                .Where(x => x.GhiChu == "Shipper")
                .Sum(x => x.SoTien);
            decimal tongChuaThu = AppProviders.HoaDons.Items
               .Where(x => !x.IsDeleted && x.Ngay == today)
               .Where(x => x.ConLai > 0 && (x.TrangThai == "Chưa thu" || x.TrangThai == "Thu một phần"))
               .Sum(g => g.ConLai);
            decimal tongTatCa = tongTienMat - tongChiTieu + tongChuaThu;

            decimal tongTraNo = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .Where(x => x.LoaiThanhToan == "Trả nợ qua ngày"
            && x.TenPhuongThucThanhToan == "Tiền mặt"
            && x.GhiChu != "Shipper"
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
            var tongKhanhGrid = new Grid();
            tongKhanhGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tongKhanhGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var tongKhanhLabel = new TextBlock
            {
                Text = "Khánh:",
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var tongKhanhValue = new TextBlock
            {
                Text = $"{tongKhanh:N0} đ",
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
            };
            Grid.SetColumn(tongKhanhLabel, 0);
            Grid.SetColumn(tongKhanhValue, 1);
            tongKhanhGrid.Children.Add(tongKhanhLabel);
            tongKhanhGrid.Children.Add(tongKhanhValue);
            MangVeStackPanel.Children.Add(tongKhanhGrid);




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
                Text = $"{tongChiTieu:N0} đ",
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
            => OpenHoaDonWithPhanLoai("Mv");

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
                // 🟟 Ngắt phát giọng đọc cũ ngay lập tức
                _cts?.Cancel();
                TTSHelper.Stop();

                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                // Reset UI
                SearchChiTietHoaDonTextBox.Visibility = Visibility.Collapsed;
                TongSoSanPhamTextBlock.Visibility = Visibility.Visible;
                TongSoSanPhamTextBlock.Text = string.Empty;
                TenHoaDonTextBlock.Text = string.Empty;
                ChiTietHoaDonListBox.ItemsSource = null;


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
                //ThongTinThanhToanTextBlock.Foreground = Brushes.Black;
                UpdateThongTinThanhToanStyle(hd);
                // gán DataContext cho GroupBox để trigger màu
                ThongTinThanhToanGroupBox.DataContext = hd;

                // build footer text
                RenderFooterPanel(ThongTinThanhToanPanel, hd, includeLine: false);
                TenHoaDonTextBlock.Text = $"{hd.Ten} - {hd.DiaChiText}"
                ;

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

                foreach (var ct in hd.ChiTietHoaDons)
                {
                    if (token.IsCancellationRequested)
                        return;

                    // 🟟 Chỉ đọc món có ghi chú
                    if (!string.IsNullOrEmpty(ct.NoteText))
                    {
                        string soLuongChu = NumberToVietnamese(ct.SoLuong);
                        string text = $"{soLuongChu} {ct.TenSanPham}";

                        await TTSHelper.DownloadAndPlayGoogleTTSAsync(text);

                        await Task.Delay(200, token);

                        await TTSHelper.DownloadAndPlayGoogleTTSAsync(ct.NoteText.Replace("#", ""));

                        await Task.Delay(400, token);
                    }
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

                var lb = new TextBlock { Text = left, FontSize = 18 };
                var spacer = new TextBlock { Text = " ", FontSize = 18 }; // chiếm Star để đẩy cột tiền sang phải
                var rb = new TextBlock { Text = right, FontSize = 18, FontWeight = FontWeights.Bold };

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
                if (!string.IsNullOrEmpty(s1)) AddGridRow("Điểm tháng này:", s1);
                var s2 = StarHelper.GetStarText(hd.DiemThangTruoc);
                if (!string.IsNullOrEmpty(s2)) AddGridRow("Điểm tháng trước:", s2);
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
                if (hd.TongNoKhachHang != hd.ConLai)
                    AddGridRow("TỔNG:", VND(hd.TongNoKhachHang + hd.ConLai));
            }

            if (includeLine) host.Children.Add(new Separator());
        }
        private void UpdateThongTinThanhToanStyle(HoaDonDto hd)
        {
            // mặc định
            ThongTinThanhToanGroupBox.Background = Brushes.LightGray;
            ThongTinThanhToanGroupBox.Foreground = Brushes.Black;

            // Ưu tiên: nếu còn nợ khách hàng > 0 thì luôn hiển thị đỏ nhạt
            if (hd.TongNoKhachHang > 0)
            {
                ThongTinThanhToanGroupBox.Background = Brushes.IndianRed;
                ThongTinThanhToanGroupBox.Foreground = Brushes.White;
                return; // dừng ở đây, không xét tiếp
            }

            // Nếu không có công nợ, xét tiếp theo trạng thái
            switch (hd.TrangThai)
            {
                case "Tiền mặt":
                    ThongTinThanhToanGroupBox.Background = Brushes.Green;
                    ThongTinThanhToanGroupBox.Foreground = Brushes.White;
                    break;

                case "Chuyển khoản":
                    ThongTinThanhToanGroupBox.Background = Brushes.LightYellow;
                    ThongTinThanhToanGroupBox.Foreground = Brushes.Black;
                    break;

                case "Banking Nhã":
                    ThongTinThanhToanGroupBox.Background = Brushes.Gold;
                    ThongTinThanhToanGroupBox.Foreground = Brushes.Black;
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
                    ThongTinThanhToanGroupBox.Foreground = Brushes.Black;
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
                    ThongTinThanhToanGroupBox.Foreground = Brushes.Black;
                    break;

                case "Thu một phần":
                    ThongTinThanhToanGroupBox.Background = Brushes.LightGreen;
                    ThongTinThanhToanGroupBox.Foreground = Brushes.Black;
                    break;

                case "Nợ một phần":
                    ThongTinThanhToanGroupBox.Background = Brushes.LightCoral;
                    ThongTinThanhToanGroupBox.Foreground = Brushes.Black; // đỏ nhạt → chữ đen vẫn đọc được
                    break;

                case "Ghi nợ":
                    ThongTinThanhToanGroupBox.Background = Brushes.IndianRed;
                    ThongTinThanhToanGroupBox.Foreground = Brushes.White;
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
        private string CleanFooterText(string content)
        {
            // gộp nhiều khoảng trắng thành 1 khoảng trắng
            var text = System.Text.RegularExpressions.Regex.Replace(content, "[ \t]+", " ");
            // gộp nhiều dòng trống liên tiếp thành 1 dòng trống
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n", "\n");
            // bỏ khoảng trắng thừa cuối dòng
            text = string.Join("\n", text.Split('\n').Select(l => l.TrimEnd()));
            return text.Trim();
        }
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
        private void ReloadHoaDonUI()
        {
            _fullHoaDonList = AppProviders.HoaDons.Items
                .Where(x => !x.IsDeleted && (x.Ngay == today
                || x.DaThuHoacGhiNo == false
                ))
                .OrderByDescending(x => x.UuTien)       // Blue lên đầu
                .ThenByDescending(x => x.IsBlue)        // Ưu tiên
                .ThenBy(x => x.TrangThai == "Chưa thu" || x.TrangThai ==
    "Thu một phần" ? 0 : 1)
                .ThenByDescending(x => x.NgayGio)       // Mới nhất
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


        private DateTime today;

        private readonly GPTService _gpt;

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    string reply = await _gpt.AskAsync("xin chào");
            //    MessageBox.Show(reply, "GPT-4o-mini");
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("❌ Lỗi: " + ex.Message);
            //}



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
            return;
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








        // 🟟 Dọn timer khi đóng form
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppShippingHelperText.DisposeDriver();

            _baoDonTimer.Stop();
            _congViecTimer.Stop();
            _updateSummaryTimer.Stop();
        }
        private void UpdateStatusIconStyle(HoaDonDto hd, IconBlock icon)
        {
            // Ẩn mặc định
            icon.Visibility = Visibility.Collapsed;
            icon.Opacity = 1; // reset khi không nhấp nháy

            if (hd == null) return;
            if (hd.DaThuHoacGhiNo) return; // đã thu/ghi nợ thì không hiện icon

            // Mặc định bật hiển thị
            icon.Visibility = Visibility.Visible;

            // Chọn icon + màu theo PhanLoai
            switch (hd.PhanLoai)
            {
                case "App":
                    icon.Icon = FontAwesome.Sharp.IconChar.Mobile;
                    icon.Foreground = Brushes.Red;
                    break;

                case "Tại Chỗ":
                    icon.Icon = FontAwesome.Sharp.IconChar.Chair;
                    icon.Foreground = Brushes.Green;
                    break;

                case "Mv":
                    icon.Icon = FontAwesome.Sharp.IconChar.ShoppingBag;
                    icon.Foreground = Brushes.Indigo;
                    break;

                case "Ship":
                    icon.Icon = FontAwesome.Sharp.IconChar.Motorcycle; // hoặc Scooter
                    icon.Foreground = Brushes.Orange;
                    break;
            }

            // 🟟 Nếu chưa thu/ghi nợ thì cho nhấp nháy
            if (!hd.DaThuHoacGhiNo)
            {
                var blink = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 0.2,
                    Duration = TimeSpan.FromSeconds(0.5),
                    AutoReverse = true,
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                };

                icon.BeginAnimation(UIElement.OpacityProperty, blink);
            }
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

            // ⚠ Dùng tên IconChar CHẮC CHẮN có trong FA6:
            // App        → MobileScreenButton
            // Tại Chỗ    → Chair
            // Mv        → BagShopping
            // Ship (moto)→ Motorcycle
            switch (hd.PhanLoai)
            {
                case "App":
                    icon.Icon = IconChar.MobileScreenButton;
                    icon.Foreground = Brushes.Red;
                    break;
                case "Tại Chỗ":
                    icon.Icon = IconChar.Chair;
                    icon.Foreground = Brushes.Green;
                    break;
                case "Mv":
                    icon.Icon = IconChar.BagShopping;
                    icon.Foreground = Brushes.Indigo;
                    break;
                case "Ship":
                    icon.Icon = IconChar.Motorcycle;
                    icon.Foreground = Brushes.Orange;
                    break;
                default:
                    icon.Icon = IconChar.Circle; // fallback
                    icon.Foreground = Brushes.Gray;
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
            icon.BeginAnimation(UIElement.OpacityProperty, blink);
        }



        private async void QuickOrderButton_Click()
        {
            if (string.IsNullOrEmpty(SearchHoaDonTextBox.Text)) return;
            string input = SearchHoaDonTextBox.Text;
            var menu = AppProviders.SanPhams.Items.Where(x => x.NgungBan == false).Select(x => x.Ten).ToList();
            var items = await _quickOrder.ParseQuickOrderAsync(input);
            if (!items.Any())
            {
                MessageBox.Show("❌ Không nhận diện được món nào.");
                return;
            }

            var dto = new HoaDonDto
            {
                PhanLoai = "Ship",
                QuickOrder = JsonSerializer.Serialize(items) // truyền order GPT qua GhiChu
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
        private async void AddAIButton_Click(object sender, RoutedEventArgs e)
        {
            await SafeButtonHandlerAsync(AppButton, async _ =>
            {
                QuickOrderButton_Click();
                SearchHoaDonTextBox.Clear();

            });
        }
        private void SearchHoaDonTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchHoaDonTextBox.Height = 32;
        }
    }
}
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using TraSuaApp.Shared.Config;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Services;
using TraSuaApp.WpfClient.AiOrdering;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;
using TraSuaApp.WpfClient.Services;
using TraSuaApp.WpfClient.Views;

namespace TraSuaApp.WpfClient.Controls
{
    public partial class MessengerTab : UserControl
    {
        private const string MessengerUrl = "https://www.messenger.com";
        private static readonly string UserDataFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "TraSuaApp.WebView2");

        private readonly QuickOrderService _quick = new(Config.apiChatGptKey);

        // Guards
        private bool _isInitializing;
        private bool _isRestarting;
        private bool _isBusy; // chặn bấm lặp khi đang chạy

        // Remember last chat title → gợi ý KH
        private string? _latestCustomerName;

        public MessengerTab()
        {
            InitializeComponent();
            Loaded += MessengerTab_Loaded;
        }

        private async void MessengerTab_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_isInitializing || WebView.CoreWebView2 != null) return;
                await InitCoreWebView2Async(UserDataFolder);
            }
            catch (Exception ex)
            {
                _ = DiscordService.SendAsync(DiscordEventType.Admin, "Init error: " + ex);
            }
        }

        // ================= WebView2 Init / Restart =================
        private async Task InitCoreWebView2Async(string userDataFolder, bool isFallback = false)
        {
            try
            {
                _isInitializing = true;

                Directory.CreateDirectory(userDataFolder);
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
                await WebView.EnsureCoreWebView2Async(env);

                HookCoreEvents();

                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

                SafeNavigateToMessenger();
            }
            catch (Exception ex)
            {
                _ = DiscordService.SendAsync(DiscordEventType.Admin,
                    $"WebView2 init failed ({(isFallback ? "fallback" : "primary")}): {ex.Message}");

                if (!isFallback)
                {
                    string fallback = Path.Combine(Path.GetTempPath(), "TraSuaApp.WebView2.Fallback");
                    await InitCoreWebView2Async(fallback, isFallback: true);
                }
                else
                {
                    MessageBox.Show("Không khởi tạo được trình duyệt nhúng (WebView2). Hãy thử mở lại ứng dụng.");
                }
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void HookCoreEvents()
        {
            if (WebView.CoreWebView2 == null) return;

            UnhookCoreEvents(); // tránh gắn trùng

            WebView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
            WebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            WebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;

            // KHÔNG gắn BrowserProcessExited để tương thích SDK WebView2 cũ
        }

        private void UnhookCoreEvents()
        {
            if (WebView.CoreWebView2 == null) return;

            try { WebView.CoreWebView2.ProcessFailed -= CoreWebView2_ProcessFailed; } catch { }
            try { WebView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted; } catch { }
            try { WebView.CoreWebView2.DocumentTitleChanged -= CoreWebView2_DocumentTitleChanged; } catch { }
        }

        private void SafeNavigateToMessenger()
        {
            try
            {
                WebView.CoreWebView2?.Navigate(MessengerUrl);
            }
            catch (Exception ex)
            {
                _ = DiscordService.SendAsync(DiscordEventType.Admin, "Navigate error: " + ex.Message);
            }
        }

        private async Task RestartWebViewAsync()
        {
            if (_isRestarting) return;
            _isRestarting = true;

            try
            {
                UnhookCoreEvents();
                await InitCoreWebView2Async(UserDataFolder);
            }
            catch (Exception ex)
            {
                _ = DiscordService.SendAsync(DiscordEventType.Admin, "RestartWebViewAsync failed: " + ex.Message);
            }
            finally
            {
                _isRestarting = false;
            }
        }



        private async Task<string> GetSelectedTextAsync()
        {
            const string js = @"(() => {
                try {
                    const s = window.getSelection();
                    return s ? s.toString() : '';
                } catch(e){ return ''; }
            })()";

            var raw = await WebView.CoreWebView2.ExecuteScriptAsync(js);
            return JsonSerializer.Deserialize<string>(raw) ?? string.Empty;
        }

        private static string CleanSelectedText(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            s = s.Replace('\u00A0', ' ')
                 .Replace("\u200B", "").Replace("\u200C", "").Replace("\u200D", "").Replace("\uFEFF", "");

            s = s.Replace("\r\n", "\n").Replace("\r", "\n");
            s = Regex.Replace(s, @"[ \t]+", " ");
            s = Regex.Replace(s, @"\n{3,}", "\n\n");

            const int maxChars = 4000;
            if (s.Length > maxChars) s = s.Substring(0, maxChars);

            return s.Trim();
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WebView.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Reload lỗi: " + ex.Message);
            }
        }

        private async void CreateOrderFromImage_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            _isBusy = true;

            var dlg = new OpenFileDialog
            {
                Title = "Chọn ảnh để tạo hoá đơn",
                Filter = "Ảnh (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files|*.*",
                InitialDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };

            if (dlg.ShowDialog() != true)
            {
                _isBusy = false;
                return;
            }

            try
            {
                // 1) Chọn khách (không bọc loader)
                var pick = new SelectCustomerDialog
                {
                    Owner = Window.GetWindow(this)
                };
                pick.KhachHangBox.SearchTextBox.Text = _latestCustomerName;
                await pick.Dispatcher.BeginInvoke(new Action(() =>
                {
                    pick.KhachHangBox.IsPopupOpen = true;
                }), System.Windows.Threading.DispatcherPriority.Background);

                KhachHangDto? kh = null;
                bool? pickResult = pick.ShowDialog();
                if (pickResult == true)
                {
                    kh = pick.SelectedKhachHang;
                }
                else if (!pick.RequestedNewCustomer)
                {
                    // Hủy
                    return;
                }

                // 2) Gọi AI bên trong loader
                (HoaDonDto? hd, string raw, List<QuickOrderDto> preds) res;

                using (BusyUI.Scope(this, sender as Button, "Đang phân tích ảnh..."))
                {
                    string? lichSuText = kh != null ? await BuildLichSuText(kh.Id) : null; // IO-bound → chỉ await
                    res = await _quick.BuildHoaDonAsync(
                        dlg.FileName, isImage: true, shortMenuFromHistory: lichSuText, khachHangId: kh?.Id);
                }

                var hd = res.hd ?? new HoaDonDto { ChiTietHoaDons = new() };
                var raw = res.raw;
                var preds = res.preds;

                // ✅ Vẫn mở form ngay cả khi không bắt được món
                if (hd.ChiTietHoaDons == null || hd.ChiTietHoaDons.Count == 0)
                {
                    hd.ChiTietHoaDons ??= new();
                }

                hd.PhanLoai = "Ship";
                hd.KhachHangId = kh?.Id;

                var mainWin = Application.Current.MainWindow;
                var win = new HoaDonEdit(hd)
                {
                    GptInputText = raw,
                    GptPredictions = preds,
                    Owner = mainWin,
                    Width = mainWin?.ActualWidth ?? 1200,
                    Height = mainWin?.ActualHeight ?? 800,
                };

                if (kh != null)
                {
                    win.ContentRendered += async (_, __) =>
                    {
                        await Task.Delay(100);
                        win.KhachHangSearchBox.SetSelectedKhachHangByIdWithoutPopup(kh.Id);
                        win.KhachHangSearchBox.TriggerSelectedEvent(kh);
                    };
                }
                else
                {
                    win.KhachHangSearchBox.SearchTextBox.Text = _latestCustomerName;
                }

                win.ShowDialog();

                mainWin?.Activate();
                mainWin?.Focus();
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Mạng chậm/AI quá tải (timeout). Sẽ mở hoá đơn trống để bạn nhập tay.");
                var hd = new HoaDonDto { PhanLoai = "Ship" };
                var mainWin = Application.Current.MainWindow;
                new HoaDonEdit(hd) { Owner = mainWin }.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tạo đơn từ ảnh lỗi: " + ex.Message);
                await DiscordService.SendAsync(DiscordEventType.Admin, ex.Message);
            }
            finally
            {
                _isBusy = false;
            }
        }

        // ================= WebView2 Events =================
        private void CoreWebView2_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
        {
            _ = DiscordService.SendAsync(DiscordEventType.Admin,
                $"WebView2 crashed: {e.ProcessFailedKind}");

            var kind = e.ProcessFailedKind.ToString();

            Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    if (string.Equals(kind, "BrowserProcessExited", StringComparison.OrdinalIgnoreCase))
                    {
                        await RestartWebViewAsync();
                    }
                    else
                    {
                        try { WebView?.Reload(); }
                        catch { await RestartWebViewAsync(); }
                    }
                }
                catch { await RestartWebViewAsync(); }
            });
        }

        private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
                _ = DiscordService.SendAsync(DiscordEventType.Admin, $"Navigation failed: {e.WebErrorStatus}");
        }

        private void CoreWebView2_DocumentTitleChanged(object? sender, object e)
        {
            var title = WebView.CoreWebView2?.DocumentTitle ?? "";
            var cut = title.Split('|')[0].Trim();
            _latestCustomerName = cut;
        }

        private async Task<string> BuildLichSuText(Guid khId)
        {
            try
            {
                var response = await ApiClient.GetAsync($"/api/Dashboard/topmenu-quickorder/{khId}");
                var info = await response.Content.ReadFromJsonAsync<string>();
                return info ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
        // ================= Buttons & Helpers =================
        private async void CreateOrderFromText_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            _isBusy = true;

            try
            {
                // 0) Lấy & làm sạch text đang bôi đen
                var text = await GetSelectedTextAsync();
                text = CleanSelectedText(text);

                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show("Hãy bôi đen đoạn văn bản trước khi tạo hoá đơn.");
                    return;
                }

                // 1) Hỏi khách (KHÔNG bọc loader)
                var pick = new SelectCustomerDialog
                {
                    Owner = Window.GetWindow(this)
                };
                pick.KhachHangBox.SearchTextBox.Text = _latestCustomerName;
                await pick.Dispatcher.BeginInvoke(new Action(() =>
                {
                    pick.KhachHangBox.IsPopupOpen = true;
                }), System.Windows.Threading.DispatcherPriority.Background);

                KhachHangDto? kh = null;
                bool? pickResult = pick.ShowDialog();
                if (pickResult == true)
                {
                    kh = pick.SelectedKhachHang;
                }
                else if (!pick.RequestedNewCustomer)
                {
                    return; // hủy
                }

                // 2) Gọi AI từ TEXT đã chọn (bọc loader) — gồm luôn tải lịch sử
                (HoaDonDto? hd, string raw, List<QuickOrderDto> preds) res;

                using (BusyUI.Scope(this, sender as Button, "Đang phân tích đoạn đã chọn..."))
                {
                    string? lichSuText = kh != null ? await BuildLichSuText(kh.Id) : null; // IO-bound
                    res = await _quick.BuildHoaDonAsync(
                        text, isImage: false, shortMenuFromHistory: lichSuText, khachHangId: kh?.Id);
                }

                var hd = res.hd ?? new HoaDonDto { ChiTietHoaDons = new() };
                var raw = res.raw;
                var preds = res.preds;


                // 3) Mở form kể cả khi AI không nhận diện được
                if (hd.ChiTietHoaDons == null || hd.ChiTietHoaDons.Count == 0)
                {
                    hd.ChiTietHoaDons ??= new();
                }

                hd.PhanLoai = "Ship";
                hd.KhachHangId = kh?.Id;

                var mainWin = Application.Current.MainWindow;
                var win = new HoaDonEdit(hd)
                {
                    GptInputText = raw,
                    GptPredictions = preds,
                    Owner = mainWin,
                    Width = mainWin?.ActualWidth ?? 1200,
                    Height = mainWin?.ActualHeight ?? 800,
                };

                if (kh != null)
                {
                    win.ContentRendered += async (_, __) =>
                    {
                        await Task.Delay(100);
                        win.KhachHangSearchBox.SetSelectedKhachHangByIdWithoutPopup(kh.Id);
                        win.KhachHangSearchBox.TriggerSelectedEvent(kh);
                    };
                }
                else
                {
                    win.KhachHangSearchBox.SearchTextBox.Text = _latestCustomerName;
                }

                win.ShowDialog();

                mainWin?.Activate();
                mainWin?.Focus();
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Mạng chậm/AI quá tải (timeout). Sẽ mở hoá đơn trống để bạn nhập tay.");
                var hd = new HoaDonDto { PhanLoai = "Ship" };
                var mainWin = Application.Current.MainWindow;
                new HoaDonEdit(hd) { Owner = mainWin }.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tạo đơn lỗi: " + ex.Message);
                await DiscordService.SendAsync(DiscordEventType.Admin, ex.Message);
            }
            finally
            {
                _isBusy = false;
            }
        }
    }
}
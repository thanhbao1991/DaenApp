using System.IO;
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
                // Mở thẳng HoaDonEdit, KHÔNG hỏi khách, KHÔNG gọi GPT ở đây
                var owner = WindowOwnerHelper.FindOwner(this);
                var hd = new HoaDonDto { PhanLoai = "Ship" };

                var win = new HoaDonEdit(
                    dto: hd,
                    gptInput: dlg.FileName,                  // truyền chuỗi đường dẫn ảnh
                    latestCustomerName: _latestCustomerName,
                    openedFromMessenger: true
                );

                WindowOwnerHelper.SetOwnerIfPossible(win, owner);
                win.WindowStartupLocation = owner != null
                    ? WindowStartupLocation.CenterOwner
                    : WindowStartupLocation.CenterScreen;

                if (owner != null)
                {
                    win.Width = owner.ActualWidth;
                    win.Height = owner.ActualHeight;
                }

                win.ShowDialog();

                owner?.Activate();
                owner?.Focus();
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Mạng chậm/AI quá tải (timeout). Sẽ mở hoá đơn trống để bạn nhập tay.");
                var hd = new HoaDonDto { PhanLoai = "Ship" };

                var owner = WindowOwnerHelper.FindOwner(this);
                var w = new HoaDonEdit(hd);
                WindowOwnerHelper.SetOwnerIfPossible(w, owner);
                w.WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
                w.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tạo đơn từ ẢNH lỗi: " + ex.Message);
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


        private async void CreateOrderFromText_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            _isBusy = true;

            try
            {
                // 1) Lấy & làm sạch text đang bôi đen
                var text = await GetSelectedTextAsync();
                text = CleanSelectedText(text);

                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show("Hãy bôi đen đoạn văn bản trước khi tạo hoá đơn.");
                    return;
                }

                // 2) Mở thẳng HoaDonEdit, KHÔNG dùng SelectCustomerDialog, KHÔNG gọi GPT ở đây
                var owner = WindowOwnerHelper.FindOwner(this);
                var hd = new HoaDonDto { PhanLoai = "Ship" };

                var win = new HoaDonEdit(
                    dto: hd,
                    gptInput: text,                          // chuỗi text từ Messenger
                    latestCustomerName: _latestCustomerName, // gợi ý tên KH từ tiêu đề chat
                    openedFromMessenger: true                 // cờ để HoaDonEdit tự chạy GPT
                );

                WindowOwnerHelper.SetOwnerIfPossible(win, owner);
                win.WindowStartupLocation = owner != null
                    ? WindowStartupLocation.CenterOwner
                    : WindowStartupLocation.CenterScreen;

                if (owner != null)
                {
                    // tuỳ chọn khớp kích thước với cửa sổ cha
                    win.Width = owner.ActualWidth;
                    win.Height = owner.ActualHeight;
                }

                win.ShowDialog();

                owner?.Activate();
                owner?.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tạo đơn từ TEXT lỗi: " + ex.Message);
                await DiscordService.SendAsync(DiscordEventType.Admin, ex.Message);
            }
            finally
            {
                _isBusy = false;
            }
        }

    }
}
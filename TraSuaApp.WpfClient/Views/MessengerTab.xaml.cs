using System.IO;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using TraSuaApp.Shared.Config;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Services;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;
using TraSuaApp.WpfClient.Ordering;
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

        public MessengerTab()
        {
            InitializeComponent();
            Loaded += MessengerTab_Loaded;
        }

        private async void MessengerTab_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (WebView.CoreWebView2 != null) return;

                Directory.CreateDirectory(UserDataFolder);
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: UserDataFolder);
                await WebView.EnsureCoreWebView2Async(env);

                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                WebView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
                WebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                WebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;

                WebView.CoreWebView2.Navigate(MessengerUrl);
            }
            catch (Exception ex)
            {
                _ = DiscordService.SendAsync(DiscordEventType.Admin, "Init error: " + ex);
            }
        }

        private async void CreateOrderFromText_Click(object sender, RoutedEventArgs e)
        {
            await CreateOrderFromSelectionAsync();
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

            // Bỏ ký tự ẩn / NBSP / zero-width
            s = s.Replace('\u00A0', ' ')
                 .Replace("\u200B", "").Replace("\u200C", "").Replace("\u200D", "").Replace("\uFEFF", "");

            // Chuẩn hoá xuống dòng + khoảng trắng
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
            var dlg = new OpenFileDialog
            {
                Title = "Chọn ảnh để tạo hoá đơn",
                Filter = "Ảnh (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files|*.*",
                InitialDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var (hd, rawInput) = await Ordering.QuickOrderHelper.RunWithLoadingAsync(
                        "Đang phân tích ảnh...",
                        () => _quick.BuildHoaDonAsync(dlg.FileName, isImage: true)
                    );

                    if (hd.ChiTietHoaDons == null || hd.ChiTietHoaDons.Count == 0)
                    {
                        MessageBox.Show("Không nhận diện được món nào trong ảnh.");
                        return;
                    }
                    hd.PhanLoai = "Ship";

                    var win = new HoaDonEdit(hd)
                    {
                        GptInputText = rawInput,
                        Owner = Window.GetWindow(this),
                        Width = Window.GetWindow(this)?.ActualWidth ?? 1200,
                        Height = Window.GetWindow(this)?.ActualHeight ?? 800,
                    };
                    win.KhachHangSearchBox.SearchTextBox.Text = _latestCustomerName;

                    win.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Tạo đơn từ ảnh lỗi: " + ex.Message);
                    await DiscordService.SendAsync(DiscordEventType.Admin, ex.Message);
                }
            }
        }

        private void CoreWebView2_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
        {
            _ = DiscordService.SendAsync(DiscordEventType.Admin, $"WebView2 crashed: {e.ProcessFailedKind}");
            Dispatcher.InvokeAsync(() => { try { WebView?.Reload(); } catch { } });
        }

        private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
                _ = DiscordService.SendAsync(DiscordEventType.Admin, $"Navigation failed: {e.WebErrorStatus}");
        }

        private string? _latestCustomerName;

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
                return info;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task CreateOrderFromSelectionAsync()
        {
            try
            {
                var text = await GetSelectedTextAsync();
                text = CleanSelectedText(text);

                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show("Hãy bôi đen đoạn văn bản trước khi tạo hoá đơn.");
                    return;
                }

                var dlg = new SelectCustomerDialog
                {
                    Owner = Window.GetWindow(this)
                };
                dlg.KhachHangBox.SearchTextBox.Text = _latestCustomerName;
                await dlg.Dispatcher.BeginInvoke(new Action(() =>
                {
                    dlg.KhachHangBox.IsPopupOpen = true;
                }), System.Windows.Threading.DispatcherPriority.Background);

                if (dlg.ShowDialog() != true)
                    return;

                var kh = dlg.SelectedKhachHang;
                string? lichSuText = null;

                if (kh != null)
                {
                    lichSuText = await BuildLichSuText(kh.Id);
                }

                var (hd, rawInput) = await Ordering.QuickOrderHelper.RunWithLoadingAsync(
                    "Đang tạo hoá đơn AI...",
                    () => _quick.BuildHoaDonAsync(text, false, lichSuText)
                );

                if (hd.ChiTietHoaDons == null || hd.ChiTietHoaDons.Count == 0)
                {
                    MessageBox.Show("Không nhận diện được món nào từ đoạn đã chọn.");
                    return;
                }

                hd.PhanLoai = "Ship";
                hd.KhachHangId = kh?.Id;

                var mainWin = Application.Current.MainWindow;

                var win = new HoaDonEdit(hd)
                {
                    GptInputText = rawInput,
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
            catch (Exception ex)
            {
                MessageBox.Show("Tạo đơn lỗi: " + ex.Message);
                await DiscordService.SendAsync(DiscordEventType.Admin, ex.Message);
            }
        }
    }
}
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Media;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.WpfClient.Controls
{
    public partial class MessengerTab : UserControl
    {
        private const string MessengerUrl = "https://www.messenger.com";
        private static readonly string UserDataFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "TraSuaApp.WebView2"); // profile cố định để giữ đăng nhập

        // Toạ độ contextmenu (CSS px) do JS gửi về
        private double _ctxClientX = 0, _ctxClientY = 0;
        private string? _ctxLink = null;

        public MessengerTab()
        {
            InitializeComponent();
            Loaded += MessengerTab_Loaded;
        }

        // ===== UnreadCount DP =====
        public int UnreadCount
        {
            get => (int)GetValue(UnreadCountProperty);
            set => SetValue(UnreadCountProperty, value);
        }
        public static readonly DependencyProperty UnreadCountProperty =
            DependencyProperty.Register(nameof(UnreadCount), typeof(int), typeof(MessengerTab),
                new PropertyMetadata(0, OnUnreadChanged));

        public void Reload() => WebView?.Reload();

        private async void MessengerTab_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (WebView.CoreWebView2 != null) return; // tránh init lại

                Directory.CreateDirectory(UserDataFolder);
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: UserDataFolder);
                await WebView.EnsureCoreWebView2Async(env);

                // Tắt menu mặc định & lắng nghe JS
                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                WebView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
                WebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

                // JS hook Notification
                await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                    (function() {
                        const OldNotify = window.Notification;
                        window.Notification = function(title, options) {
                            try {
                                window.chrome.webview.postMessage(JSON.stringify({ type: 'notify', title: title, body: options?.body || '' }));
                            } catch(e){}
                            return new OldNotify(title, options);
                        };
                        window.Notification.requestPermission = OldNotify.requestPermission.bind(OldNotify);
                    })();
                ");

                // JS hook unread
                await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                    (function () {
                        function sendCount() {
                            try {
                                var m = (document.title || '').match(/\((\d+)\)/);
                                var count = m ? parseInt(m[1], 10) : 0;
                                window.chrome.webview.postMessage(JSON.stringify({ type:'unread', count: count }));
                            } catch(e){}
                        }
                        sendCount();
                        var titleEl = document.querySelector('title');
                        if (titleEl) {
                            new MutationObserver(sendCount).observe(titleEl, { childList: true, characterData: true, subtree: true });
                        }
                        setInterval(sendCount, 5000);
                    })();
                ");

                // JS hook contextmenu
                await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                    (function() {
                        document.addEventListener('contextmenu', function(ev){
                            try{
                                ev.preventDefault();
                                const x = ev.clientX;
                                const y = ev.clientY;
                                let el = ev.target, link = '';
                                for (let i=0;i<6 && el;i++){
                                    if(el.tagName === 'A' && el.href){ link = el.href; break; }
                                    el = el.parentElement;
                                }
                                window.chrome.webview.postMessage(JSON.stringify({ type:'ctx', x:x, y:y, link:link }));
                            }catch(e){}
                            return false;
                        }, true);
                    })();
                ");

                WebView.CoreWebView2.Navigate(MessengerUrl);
            }
            catch (Exception ex)
            {
                _ = DiscordService.SendAsync(DiscordEventType.Admin, "Init error: " + ex);
            }
        }

        // ======= WebMessageReceived =======
        private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.TryGetWebMessageAsString();
                if (string.IsNullOrWhiteSpace(json)) return;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var type = root.GetProperty("type").GetString();

                if (type == "notify")
                {
                    string title = root.GetProperty("title").GetString() ?? "Messenger";
                    string body = root.GetProperty("body").GetString() ?? "";
                    _ = DiscordService.SendAsync(DiscordEventType.Admin, $"Notify: {title} - {body}");
                }
                else if (type == "unread")
                {
                    int count = root.GetProperty("count").GetInt32();
                    UnreadCount = Math.Max(0, count);
                }
                else if (type == "ctx")
                {
                    _ctxClientX = root.GetProperty("x").GetDouble();
                    _ctxClientY = root.GetProperty("y").GetDouble();
                    _ctxLink = root.GetProperty("link").GetString();

                    ShowContextMenu();
                }
            }
            catch (Exception ex)
            {
                _ = DiscordService.SendAsync(DiscordEventType.Admin, "Message error: " + ex);
            }
        }

        // ===== MENU CHUỘT PHẢI (WPF ContextMenu) =====
        private void ShowContextMenu()
        {
            var cm = new ContextMenu();

            // Copy tin nhắn
            var copyMsg = new MenuItem { Header = "Copy tin nhắn" };
            copyMsg.Click += async (_, __) =>
            {
                string js = $@"
                    (function(){{
                        var el = document.elementFromPoint({_ctxClientX.ToString(CultureInfo.InvariantCulture)}, {_ctxClientY.ToString(CultureInfo.InvariantCulture)});
                        if (!el) return '';
                        var cur = el, depth = 0;
                        while (cur && depth < 6) {{
                            if (cur.innerText && cur.innerText.trim().length > 0 && cur.tagName !== 'IMG') break;
                            cur = cur.parentElement; depth++;
                        }}
                        return (cur || el).innerText || '';
                    }})();";

                var result = await WebView.CoreWebView2.ExecuteScriptAsync(js);
                var text = JsonSerializer.Deserialize<string>(result) ?? "";
                if (!string.IsNullOrWhiteSpace(text))
                    Clipboard.SetText(text.Trim());
            };
            cm.Items.Add(copyMsg);

            // Link
            if (!string.IsNullOrEmpty(_ctxLink))
            {
                cm.Items.Add(new Separator());

                var copyLink = new MenuItem { Header = "Copy link" };
                copyLink.Click += (_, __) => Clipboard.SetText(_ctxLink!);
                cm.Items.Add(copyLink);

                var openLink = new MenuItem { Header = "Mở link" };
                openLink.Click += (_, __) =>
                {
                    try { Process.Start(new ProcessStartInfo(_ctxLink!) { UseShellExecute = true }); }
                    catch { }
                };
                cm.Items.Add(openLink);
            }

            cm.Items.Add(new Separator());

            // Reload
            var reloadItem = new MenuItem { Header = "Reload ↻" };
            reloadItem.Click += (_, __) => WebView.Reload();
            cm.Items.Add(reloadItem);

            // Hiển thị tại vị trí chuột
            cm.PlacementTarget = this;
            cm.Placement = PlacementMode.MousePoint;
            cm.IsOpen = true;
        }

        private void CoreWebView2_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
        {
            _ = DiscordService.SendAsync(DiscordEventType.Admin, $"WebView2 crashed: {e.ProcessFailedKind}");
            Dispatcher.InvokeAsync(() =>
            {
                try { WebView?.Reload(); } catch { }
            });
        }

        private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                _ = DiscordService.SendAsync(DiscordEventType.Admin, $"Navigation failed: {e.WebErrorStatus}");
            }
        }

        // ===== Khi UnreadCount đổi =====
        private static void OnUnreadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MessengerTab tab && tab.IsLoaded)
            {
                int oldVal = (int)e.OldValue;
                int newVal = (int)e.NewValue;
                var win = Window.GetWindow(tab);
                var tabItem = win?.FindName("MessengerTabItem") as TabItem;
                bool isActive = tabItem?.IsSelected == true;

                if (newVal > oldVal)
                {
                    if (!isActive)
                    {
                        SystemSounds.Exclamation.Play();
                        var badge = win?.FindName("BadgeBorder") as Border;
                        if (badge != null)
                        {
                            var sb = (Storyboard)System.Windows.Application.Current.FindResource("FlashStoryboard");
                            sb?.Begin(badge, true);
                        }
                    }
                    else
                    {
                        tab.UnreadCount = 0;
                    }
                }
            }
        }
    }
}
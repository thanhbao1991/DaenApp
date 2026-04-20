using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using TL;
using WTelegram;
using File = System.IO.File;

namespace WpfApp1
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Client? _client;
        private User? _me;

        private const string CONFIG_FILE = "config.json";
        private const string STATE_FILE = "crawl_state.json";
        private const string DOWNLOAD_FOLDER = @"C:\inetpub\wwwroot\Backend\logs";

        public ObservableCollection<DownloadItem> Downloads { get; } = new();

        private string _apiId = "";
        private string _apiHash = "";
        private string _phone = "";

        private bool _isCrawling;
        private bool _isPaused;
        private CancellationTokenSource? _cts;

        // PHƯƠNG ÁN B: Giới hạn 5 file chờ tải để đảm bảo link không bị hết hạn
        private readonly SemaphoreSlim _throttleSemaphore = new(5);
        private readonly SemaphoreSlim _downloadConcurrency = new(3); // Tải song song 3 file

        private CrawlState _state = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadConfig();
            LoadState();

            FromDatePicker.SelectedDate ??= new DateTime(2025, 4, 1);
            ToDatePicker.SelectedDate ??= DateTime.Today;
        }

        // ================= ĐIỀU KHIỂN =================
        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (_isCrawling)
            {
                _isPaused = !_isPaused;
                StatusLabel.Text = _isPaused ? "Đang tạm dừng..." : "Tiếp tục quét...";
                StartBtn.Content = _isPaused ? "Resume" : "Pause";
                return;
            }

            try
            {
                SyncUiSettings();
                SaveConfig();
                await LoginIfNeededAsync();

                _isCrawling = true;
                _isPaused = false;
                _cts = new CancellationTokenSource();
                StartBtn.Content = "Pause";

                var from = FromDatePicker.SelectedDate?.Date ?? new DateTime(2025, 4, 1);
                var to = ToDatePicker.SelectedDate?.Date ?? DateTime.Today;

                await CrawlAllJoinedDialogsAsync(from, to, _cts.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}");
            }
            finally
            {
                _isCrawling = false;
                StartBtn.Content = "Start";
                StatusLabel.Text = "Hoàn thành phiên làm việc.";
                SaveState();
            }
        }

        private async Task WaitIfPausedAsync(CancellationToken ct)
        {
            while (_isPaused) await Task.Delay(500, ct);
        }

        // ================= CRAWL LOGIC =================
        private async Task CrawlAllJoinedDialogsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct)
        {
            var targets = await GetJoinedDialogsAsync();
            foreach (var target in targets)
            {
                if (ct.IsCancellationRequested) break;
                var key = GetPeerKey(target.Peer);
                if (_state.Peers.TryGetValue(key, out var ps) && ps.IsCompleted) continue;

                CurrentGroupLabel.Text = $"Nhóm: {target.Name}";
                await CrawlSinglePeerAsync(target.Peer, target.Name, fromDate, toDate, ct);
            }
        }

        private async Task CrawlSinglePeerAsync(InputPeer peer, string groupName, DateTime fromDate, DateTime toDate, CancellationToken ct)
        {
            var key = GetPeerKey(peer);
            if (!_state.Peers.TryGetValue(key, out var peerState)) _state.Peers[key] = peerState = new();

            int offsetId = peerState.OffsetId;
            while (!ct.IsCancellationRequested)
            {
                await WaitIfPausedAsync(ct);
                await Task.Delay(600, ct); // Delay an toàn tránh Flood API

                var history = await _client!.Messages_GetHistory(peer, offset_id: offsetId, limit: 50);
                var msgs = history.Messages.OfType<Message>().ToList();
                if (msgs.Count == 0) break;

                foreach (var m in msgs)
                {
                    var day = m.date.Date;
                    CurrentDayLabel.Text = $"Ngày: {day:yyyy-MM-dd}";

                    if (day < fromDate) { peerState.IsCompleted = true; return; }

                    if (day <= toDate && m.media is MessageMediaDocument { document: Document doc })
                    {
                        // Áp dụng bộ lọc video dọc/chất lượng
                        if (IsValidVideo(doc, out var info))
                        {
                            // Đợi nếu hàng chờ tải đang đầy (> 5 file)
                            await _throttleSemaphore.WaitAsync(ct);

                            peerState.LastMessageDate = m.date;
                            _ = HandleDownloadAsync(doc, groupName, day, info);
                        }
                    }
                }

                offsetId = msgs.Last().id;
                peerState.OffsetId = offsetId;
                SaveState();
                if (history.Messages.Length < 50) { peerState.IsCompleted = true; break; }
            }
        }

        private bool IsValidVideo(Document doc, out string info)
        {
            info = "";

            if (doc.mime_type == null || !doc.mime_type.StartsWith("video"))
                return false;

            var v = doc.attributes.OfType<DocumentAttributeVideo>().FirstOrDefault();
            if (v == null)
                return false;

            // Cho cả dọc và gần dọc (đỡ miss)
            double ratio = (double)v.w / v.h;

            // chỉ loại video ngang rõ ràng
            if (ratio > 1.2) return false;

            double sizeMB = (double)doc.size / (1024 * 1024);
            double duration = v.duration;

            if (duration <= 0) return false;

            // bitrate MB/s
            double bitrate = sizeMB / duration;

            // 🟟 chỉnh ngưỡng ở đây
            if (bitrate < 0.05) // thấp quá → video mờ
                return false;

            info = $"{v.w}x{v.h} | {duration}s | {sizeMB:F1}MB | bitrate={bitrate:F3}MB/s";
            return true;
        }
        // ================= DOWNLOAD LOGIC =================
        private async Task HandleDownloadAsync(Document doc, string groupName, DateTime day, string videoInfo)
        {
            string fileName = $"{doc.id}.mp4";
            string finalPath = Path.Combine(DOWNLOAD_FOLDER, fileName);

            var item = new DownloadItem
            {
                FileName = fileName,
                GroupName = groupName,
                CrawlDay = day,
                Status = "Đang chờ...",
                TotalSize = doc.size,
                Resolution = videoInfo
            };

            if (File.Exists(finalPath))
            {
                _throttleSemaphore.Release();
                return;
            }

            Dispatcher.Invoke(() => Downloads.Insert(0, item));

            await _downloadConcurrency.WaitAsync();
            try
            {
                item.Status = "Đang tải...";
                await DownloadFileInternalAsync(doc, item);
            }
            catch (Exception ex)
            {
                item.Status = "Lỗi: " + ex.Message;
            }
            finally
            {
                _downloadConcurrency.Release();
                _throttleSemaphore.Release(); // Giải phóng cho luồng quét tiếp tục
            }
        }

        private async Task DownloadFileInternalAsync(Document doc, DownloadItem item)
        {
            Directory.CreateDirectory(DOWNLOAD_FOLDER);
            string finalPath = Path.Combine(DOWNLOAD_FOLDER, item.FileName);
            string tempPath = finalPath + ".part";

            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await _client!.DownloadFileAsync(doc, fs, progress: (done, total) =>
                {
                    item.Progress = (double)done / total * 100;
                });
            }

            if (File.Exists(finalPath)) File.Delete(finalPath);
            File.Move(tempPath, finalPath);
            item.Status = "Xong";
            item.Progress = 100;
        }

        // ================= HELPERS =================
        private async Task<List<JoinedDialogTarget>> GetJoinedDialogsAsync()
        {
            var dialogs = await _client!.Messages_GetAllDialogs();
            return dialogs.chats.Values.Select<ChatBase, JoinedDialogTarget?>(c => c switch
            {
                Channel ch => new JoinedDialogTarget(new InputPeerChannel(ch.id, ch.access_hash), ch.title),
                Chat g => new JoinedDialogTarget(new InputPeerChat(g.id), g.title),
                _ => null
            }).Where(x => x != null).Cast<JoinedDialogTarget>().ToList();
        }

        private string GetPeerKey(InputPeer peer) => peer switch
        {
            InputPeerChannel ch => $"ch_{ch.channel_id}",
            InputPeerChat c => $"chat_{c.chat_id}",
            _ => "unknown"
        };

        private void SyncUiSettings() { _apiId = ApiIdBox.Text.Trim(); _apiHash = ApiHashBox.Text.Trim(); _phone = PhoneBox.Text.Trim(); }
        private void SaveConfig() { File.WriteAllText(CONFIG_FILE, JsonSerializer.Serialize(new AppConfig { ApiId = _apiId, ApiHash = _apiHash, Phone = _phone }, new JsonSerializerOptions { WriteIndented = true })); }
        private void LoadConfig() { if (!File.Exists(CONFIG_FILE)) return; var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(CONFIG_FILE)); ApiIdBox.Text = cfg?.ApiId; ApiHashBox.Text = cfg?.ApiHash; PhoneBox.Text = cfg?.Phone; }
        private void SaveState() { File.WriteAllText(STATE_FILE, JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true })); }
        private void LoadState() { if (File.Exists(STATE_FILE)) _state = JsonSerializer.Deserialize<CrawlState>(File.ReadAllText(STATE_FILE)) ?? new(); }
        private async Task LoginIfNeededAsync() { _client ??= new Client(TgConfig); _me = await _client.LoginUserIfNeeded(); }
        private string TgConfig(string what) => what switch { "api_id" => _apiId, "api_hash" => _apiHash, "phone_number" => _phone, "verification_code" => Microsoft.VisualBasic.Interaction.InputBox("Nhập mã:"), "password" => Microsoft.VisualBasic.Interaction.InputBox("Mật khẩu 2FA:"), _ => null! };
        protected void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ================= MODELS =================
    public class DownloadItem : INotifyPropertyChanged
    {
        public string FileName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public string Resolution { get; set; } = "";
        public DateTime CrawlDay { get; set; }
        public long TotalSize { get; set; }
        private string _status = "";
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
        private double _progress;
        public double Progress { get => _progress; set { _progress = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
    public class CrawlState { public Dictionary<string, PeerState> Peers { get; set; } = new(); }
    public class PeerState { public int OffsetId { get; set; } public bool IsCompleted { get; set; } public DateTime? LastMessageDate { get; set; } }
    public class AppConfig { public string? ApiId { get; set; } public string? ApiHash { get; set; } public string? Phone { get; set; } }
    public record JoinedDialogTarget(InputPeer Peer, string Name);
}

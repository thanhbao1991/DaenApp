using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;

using TL;
using WTelegram;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        Client client;
        User me;

        const string CONFIG_FILE = "config.json";
        const string DB_FILE = "downloaded.json";

        public ObservableCollection<DownloadItem> Downloads { get; set; } = new();

        string _apiId;
        string _apiHash;
        string _phone;

        string FFMPEG_PATH => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");

        // =========================
        // QUEUE
        // =========================
        private readonly object _downloadLock = new();
        private readonly Queue<DownloadItem> _downloadQueue = new();
        private bool _isDownloading = false;

        // =========================
        // STORE
        // =========================
        private List<DownloadRecord> _cache = new();
        private readonly object _storeLock = new();

        // =========================
        // GROUP CACHE
        // =========================
        private readonly Dictionary<long, string> _groupCache = new();

        // =========================
        // DEDUP SYSTEM
        // =========================
        private readonly HashSet<string> _seen = new();
        private readonly HashSet<string> _processing = new();
        private readonly object _dedupLock = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            LoadConfig();
            LoadStore();
            AutoLogin();
        }

        // =========================
        // AUTO LOGIN
        // =========================
        private async void AutoLogin()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiId) ||
                    string.IsNullOrWhiteSpace(_apiHash) ||
                    string.IsNullOrWhiteSpace(_phone))
                    return;

                client = new Client(Config);
                me = await client.LoginUserIfNeeded();

                client.OnUpdates += async (updates) =>
                {
                    await HandleUpdatesAsync(updates);
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveConfig();

                _apiId = ApiIdBox.Text;
                _apiHash = ApiHashBox.Text;
                _phone = PhoneBox.Text;

                client = new Client(Config);
                me = await client.LoginUserIfNeeded();

                client.OnUpdates += async (updates) =>
                {
                    await HandleUpdatesAsync(updates);
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        string Config(string what)
        {
            return what switch
            {
                "api_id" => _apiId,
                "api_hash" => _apiHash,
                "phone_number" => _phone,
                "verification_code" => Microsoft.VisualBasic.Interaction.InputBox("Code"),
                "password" => Microsoft.VisualBasic.Interaction.InputBox("2FA"),
                _ => null
            };
        }

        // =========================
        // TELEGRAM UPDATE
        // =========================
        private async Task HandleUpdatesAsync(UpdatesBase updates)
        {
            foreach (var u in updates.UpdateList)
            {
                if (u is not UpdateNewMessage unm)
                    continue;

                if (unm.message is not Message m)
                    continue;

                await HandleMessage(m);
            }
        }

        // =========================
        // HANDLE MESSAGE
        // =========================
        private async Task HandleMessage(Message m)
        {
            try
            {
                if (m.media is not MessageMediaDocument md)
                    return;

                if (md.document is not Document doc)
                    return;

                if (doc.mime_type != "video/mp4")
                    return;

                var videoAttr = doc.attributes
                    .OfType<DocumentAttributeVideo>()
                    .FirstOrDefault();

                if (videoAttr == null)
                    return;

                // chỉ video dọc
                if (videoAttr.h <= videoAttr.w)
                    return;

                // tối thiểu 720p dọc
                if (videoAttr.h < 1280 || videoAttr.w < 720)
                    return;

                string fileKey = doc.id.ToString();
                string groupName = await GetGroupNameCached(m);

                lock (_dedupLock)
                {
                    if (_seen.Contains(fileKey) ||
                        _cache.Any(x => x.FileName == fileKey + ".mp4") ||
                        _processing.Contains(fileKey))
                    {
                        return;
                    }

                    _seen.Add(fileKey);
                    _processing.Add(fileKey);
                }

                var item = new DownloadItem
                {
                    FileName = fileKey,
                    GroupName = groupName,
                    Status = "⏳ QUEUED",
                    SourceMessage = m
                };

                Dispatcher.Invoke(() => Downloads.Add(item));

                lock (_downloadLock)
                {
                    _downloadQueue.Enqueue(item);

                    if (!_isDownloading)
                        _ = Task.Run(ProcessQueue);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        // =========================
        // GROUP NAME
        // =========================
        private async Task<string> GetGroupNameCached(Message m)
        {
            try
            {
                long id =
                    (m.peer_id as PeerChannel)?.channel_id ??
                    (m.peer_id as PeerChat)?.chat_id ??
                    (m.peer_id as PeerUser)?.user_id ??
                    0;

                if (_groupCache.TryGetValue(id, out var cached))
                    return cached;

                string name = "unknown";

                if (m.peer_id is PeerChannel pc)
                {
                    var chats = await client.Messages_GetAllChats();
                    var channel = chats.chats.Values
                        .OfType<Channel>()
                        .FirstOrDefault(x => x.id == pc.channel_id);

                    name = channel?.title ?? $"channel_{pc.channel_id}";
                }
                else if (m.peer_id is PeerChat cc)
                {
                    var chats = await client.Messages_GetAllChats();
                    var chat = chats.chats.Values
                        .OfType<Chat>()
                        .FirstOrDefault(x => x.id == cc.chat_id);

                    name = chat?.title ?? $"group_{cc.chat_id}";
                }

                _groupCache[id] = name;
                return name;
            }
            catch
            {
                return "unknown";
            }
        }

        // =========================
        // QUEUE PROCESS
        // =========================
        private async Task ProcessQueue()
        {
            _isDownloading = true;

            try
            {
                while (true)
                {
                    DownloadItem item = null;

                    lock (_downloadLock)
                    {
                        if (_downloadQueue.Count > 0)
                            item = _downloadQueue.Dequeue();
                        else
                            break;
                    }

                    if (item != null)
                        await DownloadCore(item);
                }
            }
            finally
            {
                _isDownloading = false;
            }
        }

        // =========================
        // DOWNLOAD CORE (NO CONVERT)
        // =========================
        private async Task DownloadCore(DownloadItem item)
        {
            try
            {
                item.Status = "⬇ DOWNLOADING";

                var folder = @"C:\inetpub\wwwroot\Backend\logs";
                Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, item.FileName + ".mp4");

                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    var m = item.SourceMessage;

                    if (m.media is MessageMediaDocument md &&
                        md.document is Document doc)
                    {
                        await client.DownloadFileAsync(doc, fs);
                    }
                    else return;
                }

                item.Status = "✅ READY";

                AddRecord(item.FileName + ".mp4", item.GroupName);
            }
            catch (Exception ex)
            {
                item.Status = "❌ FAILED: " + ex.Message;
            }
            finally
            {
                lock (_dedupLock)
                {
                    _processing.Remove(item.FileName);
                }
            }
        }

        // =========================
        // STORE
        // =========================
        private void LoadStore()
        {
            try
            {
                if (!File.Exists(DB_FILE))
                {
                    _cache = new List<DownloadRecord>();
                    File.WriteAllText(DB_FILE, "[]");
                    return;
                }

                _cache = JsonSerializer.Deserialize<List<DownloadRecord>>(
                    File.ReadAllText(DB_FILE)
                ) ?? new List<DownloadRecord>();
            }
            catch
            {
                _cache = new List<DownloadRecord>();
            }
        }

        private void SaveStore()
        {
            File.WriteAllText(DB_FILE,
                JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true }));
        }

        private void AddRecord(string fileName, string groupName)
        {
            lock (_storeLock)
            {
                if (_cache.Any(x => x.FileName == fileName))
                    return;

                _cache.Add(new DownloadRecord
                {
                    FileName = fileName,
                    GroupName = groupName
                });

                SaveStore();
            }
        }

        // =========================
        // CONFIG
        // =========================
        void SaveConfig()
        {
            var cfg = new AppConfig
            {
                ApiId = ApiIdBox.Text,
                ApiHash = ApiHashBox.Text,
                Phone = PhoneBox.Text
            };

            File.WriteAllText(CONFIG_FILE,
                JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
        }

        void LoadConfig()
        {
            if (!File.Exists(CONFIG_FILE)) return;

            var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(CONFIG_FILE));
            if (cfg == null) return;

            _apiId = cfg.ApiId;
            _apiHash = cfg.ApiHash;
            _phone = cfg.Phone;

            ApiIdBox.Text = cfg.ApiId;
            ApiHashBox.Text = cfg.ApiHash;
            PhoneBox.Text = cfg.Phone;
        }
    }

    // =========================
    // MODELS
    // =========================
    public class DownloadItem : INotifyPropertyChanged
    {
        public string FileName { get; set; }
        public string GroupName { get; set; }
        public Message SourceMessage { get; set; }

        string status;
        public string Status
        {
            get => status;
            set { status = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class DownloadRecord
    {
        public string FileName { get; set; }
        public string GroupName { get; set; }
    }

    public class AppConfig
    {
        public string ApiId { get; set; }
        public string ApiHash { get; set; }
        public string Phone { get; set; }
    }
}
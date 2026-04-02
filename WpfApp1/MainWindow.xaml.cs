using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using TL;
using WTelegram;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        // ================= CORE =================
        private Client _client;
        private User _me;

        private const string CONFIG_FILE = "config.json";
        private const string CRAWL_STATE_FILE = "crawlstate.json";
        private const string DOWNLOAD_FOLDER = @"C:\inetpub\wwwroot\Backend\logs";

        public ObservableCollection<DownloadItem> Downloads { get; } = new();

        private string _apiId, _apiHash, _phone;
        private string _botToken;

        // ================= BOT =================
        private const string CHAT_ID = "8368218219";
        private const string BOT_USERNAME = "meome0_bot";

        // ================= SHARED HTTP =================
        private static readonly HttpClient _http = new();

        // ================= PENDING APPROVE =================
        private readonly ConcurrentDictionary<string, DownloadItem> _pending = new();
        private long _updateOffset = 0;

        // ================= BOT QUEUE =================
        private readonly Queue<DownloadItem> _botQueue = new();
        private readonly object _botLock = new();
        private bool _isBotRunning;

        // Queue key để map đúng file_id của tin forwarded từ bot
        private readonly Queue<string> _botKeyQueue = new();
        private readonly object _botKeyLock = new();

        // ================= DOWNLOAD QUEUE =================
        private readonly Queue<DownloadItem> _downloadQueue = new();
        private readonly object _downloadLock = new();
        private bool _isDownloading;

        // ================= DEDUP =================
        private readonly HashSet<string> _seen = new();
        private readonly HashSet<string> _processing = new();
        private readonly object _dedupLock = new();

        // ================= GROUP CACHE =================
        private readonly Dictionary<long, string> _groupCache = new();

        // ================= CRAWL STATE =================
        private readonly object _crawlLock = new();
        private bool _isCrawling;
        private CrawlState _crawlState;

        // ================= CONTINUE BUTTON =================
        private readonly object _continueLock = new();
        private TaskCompletionSource<bool> _continueTcs;

        // ================= BATCH QUOTA =================
        private const int BATCH_SIZE = 50;
        private int _resumeQuota = 0;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            LoadConfig();
            LoadCrawlStateToUi();
            SetDefaultDates();
            SyncUiSettings();

            AutoLogin();
            StartBotPolling();
        }

        private void SetDefaultDates()
        {
            if (CrawlDatePicker.SelectedDate is null)
                CrawlDatePicker.SelectedDate = DateTime.Today;
        }

        private void SyncUiSettings()
        {
            _botToken = BotTokenBox.Text?.Trim() ?? "";
            _apiId = ApiIdBox.Text?.Trim() ?? "";
            _apiHash = ApiHashBox.Text?.Trim() ?? "";
            _phone = PhoneBox.Text?.Trim() ?? "";
        }

        private void UpdateCrawlUi(string status, DateTime? currentDay = null, string resume = null, string note = null)
        {
            Dispatcher.Invoke(() =>
            {
                CrawlStatusText.Text = status ?? "Chưa chạy";

                CrawlDayText.Text = currentDay.HasValue
                    ? $"Ngày hiện tại: {currentDay.Value:yyyy-MM-dd}"
                    : "Ngày hiện tại: -";

                CrawlResumeText.Text = string.IsNullOrWhiteSpace(resume)
                    ? "Resume: -"
                    : $"Resume: {resume}";

                CrawlNoteText.Text = string.IsNullOrWhiteSpace(note)
                    ? "Ghi chú: -"
                    : $"Ghi chú: {note}";
            });
        }

        // ─────────────────────────────────────────────────────────
        //  LOGIN
        // ─────────────────────────────────────────────────────────
        private async Task LoginIfNeededAsync()
        {
            if (_client != null && _me != null)
                return;

            if (string.IsNullOrWhiteSpace(_apiId) ||
                string.IsNullOrWhiteSpace(_apiHash) ||
                string.IsNullOrWhiteSpace(_phone))
                return;

            _client ??= new Client(TgConfig);
            _me = await _client.LoginUserIfNeeded();
            _client.OnUpdates -= OnUpdates;
            _client.OnUpdates += OnUpdates;

            await TryResumeCrawlAsync();
        }

        private async void AutoLogin()
        {
            try
            {
                await LoginIfNeededAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AutoLogin] {ex.Message}");
            }
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SyncUiSettings();
                SaveConfig();

                await LoginIfNeededAsync();

                if (_client is null || _me is null)
                {
                    MessageBox.Show("Thiếu API ID / API Hash / Phone hoặc chưa login được.");
                    return;
                }

                MessageBox.Show("Đã kết nối Telegram.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void Crawl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SyncUiSettings();
                SaveConfig();

                await LoginIfNeededAsync();

                if (_client is null || _me is null)
                {
                    MessageBox.Show("Bạn cần login trước.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(GroupInputBox.Text))
                {
                    MessageBox.Show("Nhập username hoặc ID nhóm cần crawl.");
                    return;
                }

                if (_isCrawling)
                {
                    if (ResumeIfPaused())
                        return;

                    MessageBox.Show("Đang crawl rồi.");
                    return;
                }

                _crawlState = new CrawlState
                {
                    Group = GroupInputBox.Text.Trim(),
                    CursorMessageId = 0,
                    CurrentDate = CrawlDatePicker.SelectedDate?.Date ?? DateTime.Today,
                    TargetDate = DateTime.MinValue,
                    EmptyDayStreak = 0,
                    IsRunning = true
                };

                SaveCrawlState(_crawlState);

                lock (_continueLock)
                {
                    _continueTcs = null;
                }

                _resumeQuota = BATCH_SIZE;

                Dispatcher.Invoke(() => ContinueButton.IsEnabled = false);

                UpdateCrawlUi(
                    $"Đang bắt đầu crawl, mỗi lượt {BATCH_SIZE} video...",
                    _crawlState.CurrentDate,
                    "cursor = 0",
                    "crawl theo batch");

                await CrawlGroupHistoryByMessageIdLoop(_crawlState);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            ResumeIfPaused();
        }

        private bool ResumeIfPaused()
        {
            TaskCompletionSource<bool> tcs = null;

            lock (_continueLock)
            {
                if (_continueTcs != null && !_continueTcs.Task.IsCompleted)
                {
                    _resumeQuota = BATCH_SIZE;
                    tcs = _continueTcs;
                }
            }

            if (tcs is null)
                return false;

            Dispatcher.Invoke(() => ContinueButton.IsEnabled = false);

            UpdateCrawlUi(
                $"Đang chạy thêm {BATCH_SIZE} video...",
                _crawlState?.CurrentDate,
                $"cursor={_crawlState?.CursorMessageId ?? 0}",
                "resume batch");

            tcs.TrySetResult(true);
            return true;
        }

        private string TgConfig(string what) => what switch
        {
            "api_id" => _apiId,
            "api_hash" => _apiHash,
            "phone_number" => _phone,
            "verification_code" => Microsoft.VisualBasic.Interaction.InputBox("Code"),
            "password" => Microsoft.VisualBasic.Interaction.InputBox("2FA"),
            _ => null
        };

        // ─────────────────────────────────────────────────────────
        //  CRAWL BY MESSAGE_ID
        // ─────────────────────────────────────────────────────────
        private async Task TryResumeCrawlAsync()
        {
            try
            {
                if (_isCrawling)
                    return;

                var state = LoadCrawlState();
                if (state is null || !state.IsRunning)
                    return;

                if (string.IsNullOrWhiteSpace(state.Group))
                    return;

                _crawlState = state;
                GroupInputBox.Text = state.Group;

                var selectedDate = state.TargetDate != default
                    ? state.TargetDate
                    : (state.CurrentDate == default ? DateTime.Today : state.CurrentDate);

                CrawlDatePicker.SelectedDate = selectedDate;

                Dispatcher.Invoke(() => ContinueButton.IsEnabled = false);

                UpdateCrawlUi(
                    "Đang resume crawl...",
                    state.CurrentDate,
                    $"cursor={state.CursorMessageId}",
                    $"mốc crawl: {selectedDate:yyyy-MM-dd}");

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await CrawlGroupHistoryByMessageIdLoop(state);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ResumeCrawl] {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TryResumeCrawlAsync] {ex.Message}");
            }
        }

        private async Task CrawlGroupHistoryByMessageIdLoop(CrawlState state)
        {
            lock (_crawlLock)
            {
                if (_isCrawling)
                    return;

                _isCrawling = true;
            }

            try
            {
                if (_client is null)
                    return;

                var peer = await ResolveCrawlPeerAsync(state.Group);
                if (peer is null)
                {
                    UpdateCrawlUi("Không resolve được group/channel.", null, "failed", "peer null");
                    return;
                }

                int offsetId = state.CursorMessageId;
                const int limit = 100;

                DateTime? currentDay = null;
                bool currentDayHasVideo = false;

                while (true)
                {
                    UpdateCrawlUi(
                        $"Đang crawl từ message_id {offsetId}...",
                        currentDay,
                        $"cursor={offsetId}",
                        "crawl batch");

                    int lastOffsetId = offsetId;

                    var history = await _client.Messages_GetHistory(
                        peer,
                        offset_id: offsetId,
                        add_offset: 0,
                        limit: limit,
                        max_id: 0,
                        min_id: 0,
                        hash: 0
                    );

                    var msgs = history.Messages
                        .OfType<Message>()
                        .OrderByDescending(m => m.id)
                        .ToList();

                    if (msgs.Count == 0)
                    {
                        if (currentDay.HasValue)
                            FinalizeCrawlDay(state, currentDay.Value, currentDayHasVideo);

                        break;
                    }

                    foreach (var m in msgs)
                    {
                        var msgDay = m.date.Date;

                        if (!currentDay.HasValue)
                        {
                            currentDay = msgDay;
                            currentDayHasVideo = false;
                        }
                        else if (msgDay != currentDay.Value)
                        {
                            if (FinalizeCrawlDay(state, currentDay.Value, currentDayHasVideo))
                                return;

                            currentDay = msgDay;
                            currentDayHasVideo = false;
                        }

                        bool validVideo = await HandleMessage(m, msgDay);
                        if (validVideo)
                        {
                            currentDayHasVideo = true;

                            if (_resumeQuota > 0)
                            {
                                _resumeQuota--;

                                if (_resumeQuota == 0)
                                {
                                    state.CursorMessageId = m.id;
                                    state.CurrentDate = msgDay;
                                    state.IsRunning = true;
                                    SaveCrawlState(state);

                                    UpdateCrawlUi(
                                        $"Đã crawl đủ {BATCH_SIZE} video, tạm dừng.",
                                        msgDay,
                                        $"cursor={offsetId}",
                                        "bấm Crawl / Resume để chạy tiếp");

                                    await PauseForContinueAsync(msgDay);
                                }
                            }
                        }

                        offsetId = m.id;
                        state.CursorMessageId = offsetId;
                        state.CurrentDate = msgDay;
                        state.IsRunning = true;

                        SaveCrawlState(state);
                    }

                    if (offsetId == lastOffsetId)
                    {
                        if (currentDay.HasValue)
                            FinalizeCrawlDay(state, currentDay.Value, currentDayHasVideo);

                        break;
                    }
                }

                state.IsRunning = false;
                SaveCrawlState(state);

                UpdateCrawlUi(
                    "Đã crawl xong.",
                    currentDay,
                    $"cursor={state.CursorMessageId}",
                    "done");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Crawl] {ex.Message}");
                UpdateCrawlUi(
                    $"Lỗi: {ex.Message}",
                    state?.CurrentDate,
                    "resume được",
                    "error");
            }
            finally
            {
                Dispatcher.Invoke(() => ContinueButton.IsEnabled = false);

                lock (_crawlLock)
                    _isCrawling = false;
            }
        }

        private async Task PauseForContinueAsync(DateTime day)
        {
            TaskCompletionSource<bool> tcs;

            lock (_continueLock)
            {
                if (_continueTcs != null && !_continueTcs.Task.IsCompleted)
                {
                    tcs = _continueTcs;
                }
                else
                {
                    _continueTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    tcs = _continueTcs;
                }
            }

            Dispatcher.Invoke(() => ContinueButton.IsEnabled = true);

            await tcs.Task;

            lock (_continueLock)
            {
                if (ReferenceEquals(_continueTcs, tcs))
                    _continueTcs = null;
            }

            Dispatcher.Invoke(() => ContinueButton.IsEnabled = false);
        }

        private bool FinalizeCrawlDay(CrawlState state, DateTime day, bool dayHasVideo)
        {
            if (dayHasVideo)
                state.EmptyDayStreak = 0;
            else
                state.EmptyDayStreak++;

            state.CurrentDate = day;
            state.IsRunning = true;
            SaveCrawlState(state);

            return false;
        }

        private async Task<InputPeer> ResolveCrawlPeerAsync(string usernameOrId)
        {
            var raw = (usernameOrId ?? "").Trim().TrimStart('@');

            try
            {
                var resolved = await _client.Contacts_ResolveUsername(raw);

                var ch = resolved.chats.Values.OfType<Channel>().FirstOrDefault();
                if (ch != null)
                    return new InputPeerChannel(ch.id, ch.access_hash);

                var grp = resolved.chats.Values.OfType<Chat>().FirstOrDefault();
                if (grp != null)
                    return new InputPeerChat(grp.id);
            }
            catch { }

            var dialogs = await _client.Messages_GetAllDialogs();

            foreach (var chat in dialogs.chats.Values)
            {
                if (chat is Channel ch && ch.title.Contains(raw, StringComparison.OrdinalIgnoreCase))
                    return new InputPeerChannel(ch.id, ch.access_hash);

                if (chat is Chat g && g.title.Contains(raw, StringComparison.OrdinalIgnoreCase))
                    return new InputPeerChat(g.id);
            }

            return null;
        }

        // ─────────────────────────────────────────────────────────
        //  TELEGRAM UPDATE HANDLER
        // ─────────────────────────────────────────────────────────
        private async Task OnUpdates(UpdatesBase updates)
        {
            foreach (var u in updates.UpdateList)
            {
                if (u is UpdateNewMessage { message: Message m })
                    await HandleMessage(m);
            }
        }

        private static string GetTargetFilePath(string fileKey)
            => Path.Combine(DOWNLOAD_FOLDER, fileKey + ".mp4");

        private async Task<bool> HandleMessage(Message m, DateTime? crawlDay = null)
        {
            try
            {
                if (!TryGetValidVideo(m, out var doc, out var videoAttr))
                    return false;

                string fileKey = doc.id.ToString();
                string filePath = GetTargetFilePath(fileKey);

                lock (_dedupLock)
                {
                    if (_seen.Contains(fileKey) || _processing.Contains(fileKey))
                        return true;

                    _seen.Add(fileKey);
                    _processing.Add(fileKey);
                }

                if (File.Exists(filePath))
                {
                    lock (_dedupLock)
                        _processing.Remove(fileKey);

                    Debug.WriteLine($"[SKIP] File already exists: {filePath}");
                    return true;
                }

                string groupName = await GetGroupNameCached(m);

                var item = new DownloadItem
                {
                    FileName = fileKey,
                    GroupName = groupName,
                    SourceMessage = m,
                    Status = "⏳ WAIT APPROVE",
                    Progress = 0,
                    CrawlDay = crawlDay?.Date
                };

                Dispatcher.Invoke(() => Downloads.Add(item));

                EnqueueBot(item);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HandleMessage] {ex.Message}");
                return false;
            }
        }

        private bool TryGetValidVideo(Message m, out Document doc, out DocumentAttributeVideo videoAttr)
        {
            doc = null;
            videoAttr = null;

            if (m.media is not MessageMediaDocument { document: Document d })
                return false;

            if (d.mime_type != "video/mp4")
                return false;

            var va = d.attributes.OfType<DocumentAttributeVideo>().FirstOrDefault();
            if (va is null)
                return false;

            int w = va.w;
            int h = va.h;
            long sizeMB = d.size / (1024 * 1024);

            if (h <= w) return false;
            double ratio = (double)w / h;
            if (ratio > 0.7) return false;

            if (w < 720 || h < 1280) return false;
            if (va.duration < 8) return false;
            if (sizeMB < 5) return false;
            if (sizeMB > 300) return false;

            doc = d;
            videoAttr = va;
            return true;
        }

        // ─────────────────────────────────────────────────────────
        //  BOT QUEUE
        // ─────────────────────────────────────────────────────────
        private void EnqueueBot(DownloadItem item)
        {
            lock (_botLock)
            {
                _botQueue.Enqueue(item);

                if (_isBotRunning)
                    return;

                _isBotRunning = true;
                _ = Task.Run(ProcessBotQueue);
            }
        }

        private async Task ProcessBotQueue()
        {
            try
            {
                while (true)
                {
                    DownloadItem item;

                    lock (_botLock)
                    {
                        if (_botQueue.Count == 0)
                            break;

                        item = _botQueue.Dequeue();
                    }

                    while (true)
                    {
                        bool success = await SendForwardThenApprove(item);

                        if (success)
                            break;

                        item.Status = "❌ BOT ERROR → retry sau 5 phút";
                        await Task.Delay(TimeSpan.FromMinutes(5));
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
            finally
            {
                lock (_botLock)
                    _isBotRunning = false;
            }
        }

        private async Task<bool> SendForwardThenApprove(DownloadItem item)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_botToken))
                    throw new InvalidOperationException("Bot token trống.");

                var resolved = await _client.Contacts_ResolveUsername(BOT_USERNAME);
                var bot = resolved.users.Values.First();
                var botPeer = new InputPeerUser(bot.id, bot.access_hash);

                var dialogs = await _client.Messages_GetAllDialogs();
                var fromPeer = ResolveFromPeer(item.SourceMessage, dialogs);

                if (fromPeer is null)
                {
                    item.Status = "❌ PEER NOT FOUND";
                    _pending.TryRemove(item.FileName, out _);
                    return true;
                }

                _pending[item.FileName] = item;

                EnqueueBotKey(item.FileName);

                await _client.Messages_ForwardMessages(
                    from_peer: fromPeer,
                    id: new[] { item.SourceMessage.id },
                    random_id: new[] { Helpers.RandomLong() },
                    to_peer: botPeer
                );

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SendForwardThenApprove] {ex.Message}");
                item.Status = "❌ BOT ERROR";

                _pending.TryRemove(item.FileName, out _);
                RemoveBotKeyIfFront(item.FileName);

                lock (_dedupLock)
                    _processing.Remove(item.FileName);

                return false;
            }
        }

        private void EnqueueBotKey(string key)
        {
            lock (_botKeyLock)
            {
                _botKeyQueue.Enqueue(key);
            }
        }

        private void RemoveBotKeyIfFront(string key)
        {
            lock (_botKeyLock)
            {
                if (_botKeyQueue.Count == 0)
                    return;

                if (_botKeyQueue.Peek() == key)
                    _botKeyQueue.Dequeue();
            }
        }

        private string DequeueNextBotKey()
        {
            lock (_botKeyLock)
            {
                if (_botKeyQueue.Count == 0)
                    return null;

                return _botKeyQueue.Dequeue();
            }
        }

        // ─────────────────────────────────────────────────────────
        //  BOT POLLING
        // ─────────────────────────────────────────────────────────
        private void StartBotPolling()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(_botToken))
                        {
                            await Task.Delay(1000);
                            continue;
                        }

                        var res = await _http.GetStringAsync(
                            $"https://api.telegram.org/bot{_botToken}/getUpdates?offset={_updateOffset + 1}");

                        using var json = JsonDocument.Parse(res);

                        foreach (var upd in json.RootElement.GetProperty("result").EnumerateArray())
                        {
                            _updateOffset = upd.GetProperty("update_id").GetInt64();

                            if (upd.TryGetProperty("callback_query", out var cb))
                            {
                                var data = cb.GetProperty("data").GetString() ?? "";
                                long chatId = cb.GetProperty("message").GetProperty("chat").GetProperty("id").GetInt64();
                                int messageId = cb.GetProperty("message").GetProperty("message_id").GetInt32();

                                if (data.StartsWith("yes_"))
                                    await ApproveAsync(data[4..], chatId, messageId);
                                else if (data.StartsWith("no_"))
                                    await RejectAsync(data[3..], chatId, messageId);

                                continue;
                            }

                            if (upd.TryGetProperty("message", out var msg))
                            {
                                await HandleBotIncomingMessageAsync(msg);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Polling] {ex.Message}");
                    }

                    await Task.Delay(1000);
                }
            });
        }

        private async Task HandleBotIncomingMessageAsync(JsonElement msg)
        {
            try
            {
                if (msg.TryGetProperty("reply_markup", out _))
                    return;

                if (!TryExtractBotFileIdAndType(msg, out var fileId, out var isVideo))
                    return;

                string key = DequeueNextBotKey();
                if (string.IsNullOrWhiteSpace(key))
                    return;

                if (!_pending.TryGetValue(key, out var item))
                    return;

                long sourceChatId = msg.GetProperty("chat").GetProperty("id").GetInt64();
                int sourceMessageId = msg.GetProperty("message_id").GetInt32();

                bool sent = false;
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    sent = await SendApproveMediaMessageAsync(item, fileId, isVideo);
                    if (sent)
                        break;

                    await Task.Delay(TimeSpan.FromSeconds(2));
                }

                if (!sent)
                {
                    item.Status = "❌ APPROVE SEND FAILED";
                    return;
                }

                try
                {
                    await DeleteTelegramMessageAsync(sourceChatId, sourceMessageId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Delete bot source] {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HandleBotIncomingMessageAsync] {ex.Message}");
            }
        }

        private bool TryExtractBotFileIdAndType(JsonElement msg, out string fileId, out bool isVideo)
        {
            fileId = null;
            isVideo = false;

            if (msg.TryGetProperty("video", out var video))
            {
                if (video.TryGetProperty("file_id", out var id))
                {
                    fileId = id.GetString();
                    isVideo = true;
                    return !string.IsNullOrWhiteSpace(fileId);
                }
            }

            if (msg.TryGetProperty("document", out var doc))
            {
                if (doc.TryGetProperty("file_id", out var id))
                {
                    fileId = id.GetString();
                    isVideo = false;
                    return !string.IsNullOrWhiteSpace(fileId);
                }
            }

            return false;
        }

        private async Task<bool> SendApproveMediaMessageAsync(DownloadItem item, string fileId, bool isVideo)
        {
            try
            {
                var keyboard = new
                {
                    inline_keyboard = new object[][]
                    {
                        new object[]
                        {
                            new { text = "✅ YES", callback_data = "yes_" + item.FileName },
                            new { text = "❌ NO",  callback_data = "no_"  + item.FileName }
                        }
                    }
                };

                var caption = $"🟟 {item.GroupName}\n{item.FileName}";

                var fields = new List<KeyValuePair<string, string>>
                {
                    new("chat_id", CHAT_ID),
                    new("caption", caption),
                    new("reply_markup", JsonSerializer.Serialize(keyboard))
                };

                string endpoint;

                if (isVideo)
                {
                    endpoint = "sendVideo";
                    fields.Add(new KeyValuePair<string, string>("video", fileId));
                    fields.Add(new KeyValuePair<string, string>("supports_streaming", "true"));
                }
                else
                {
                    endpoint = "sendDocument";
                    fields.Add(new KeyValuePair<string, string>("document", fileId));
                }

                using var resp = await _http.PostAsync(
                    $"https://api.telegram.org/bot{_botToken}/{endpoint}",
                    new FormUrlEncodedContent(fields));

                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    throw new Exception(body);

                using var json = JsonDocument.Parse(body);
                var result = json.RootElement.GetProperty("result");

                item.ApproveChatId = result.GetProperty("chat").GetProperty("id").GetInt64();
                item.ApproveMessageId = result.GetProperty("message_id").GetInt32();

                item.Status = "⏳ WAIT APPROVE";
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SendApproveMediaMessageAsync] {ex.Message}");
                return false;
            }
        }

        private async Task ApproveAsync(string key, long approveChatId, int approveMessageId)
        {
            if (!_pending.TryRemove(key, out var item))
                return;

            item.Status = "⏳ APPROVED";

            try
            {
                await DeleteTelegramMessageAsync(approveChatId, approveMessageId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Approve delete] {ex.Message}");
            }

            lock (_downloadLock)
            {
                _downloadQueue.Enqueue(item);

                if (_isDownloading)
                    return;

                _isDownloading = true;
                _ = Task.Run(ProcessDownloadQueue);
            }
        }

        private async Task RejectAsync(string key, long rejectChatId, int rejectMessageId)
        {
            if (!_pending.TryRemove(key, out var item))
                return;

            item.Status = "❌ REJECTED";

            try
            {
                await DeleteTelegramMessageAsync(rejectChatId, rejectMessageId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Reject delete] {ex.Message}");
            }

            lock (_dedupLock)
                _processing.Remove(item.FileName);

            FinalizeCrawlItem(item);
        }

        private async Task<bool> DeleteTelegramMessageAsync(long chatId, int messageId)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["chat_id"] = chatId.ToString(),
                ["message_id"] = messageId.ToString()
            });

            using var resp = await _http.PostAsync(
                $"https://api.telegram.org/bot{_botToken}/deleteMessage",
                content);

            return resp.IsSuccessStatusCode;
        }

        // ─────────────────────────────────────────────────────────
        //  DOWNLOAD QUEUE
        // ─────────────────────────────────────────────────────────
        private async Task ProcessDownloadQueue()
        {
            try
            {
                while (true)
                {
                    DownloadItem item;

                    lock (_downloadLock)
                    {
                        if (_downloadQueue.Count == 0)
                            break;

                        item = _downloadQueue.Dequeue();
                    }

                    await DownloadCore(item);
                }
            }
            finally
            {
                lock (_downloadLock)
                    _isDownloading = false;
            }
        }

        private async Task DownloadCore(DownloadItem item)
        {
            var finalPath = GetTargetFilePath(item.FileName);
            var tempPath = finalPath + ".part";

            const int maxRetry = 3;
            const int timeoutSeconds = 300;
            const int stallSeconds = 20;

            try
            {
                Directory.CreateDirectory(DOWNLOAD_FOLDER);

                for (int attempt = 1; attempt <= maxRetry; attempt++)
                {
                    try
                    {
                        item.Status = $"⬇ DOWNLOADING ({attempt}/{maxRetry})";
                        item.Progress = 0;

                        if (File.Exists(finalPath))
                        {
                            item.Status = "⏭ ALREADY EXISTS";
                            item.Progress = 100;
                            return;
                        }

                        if (File.Exists(tempPath))
                            File.Delete(tempPath);

                        if (item.SourceMessage.media is not MessageMediaDocument { document: Document doc })
                            throw new Exception("No document");

                        long lastBytes = 0;
                        long lastBytesForSpeed = 0;

                        DateTime lastProgressTime = DateTime.Now;
                        DateTime lastSpeedCheck = DateTime.Now;

                        using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var downloadTask = _client.DownloadFileAsync(doc, fs, progress: (current, total) =>
                            {
                                var now = DateTime.Now;

                                if (total > 0)
                                    item.Progress = (int)(current * 100 / total);

                                if (current != lastBytes)
                                {
                                    lastBytes = current;
                                    lastProgressTime = now;
                                }

                                if ((now - lastSpeedCheck).TotalSeconds >= 1)
                                {
                                    var delta = current - lastBytesForSpeed;
                                    var seconds = (now - lastSpeedCheck).TotalSeconds;
                                    var speed = delta / Math.Max(seconds, 1);

                                    lastBytesForSpeed = current;
                                    lastSpeedCheck = now;

                                    item.Status = $"⬇ {item.Progress}% - {(speed / 1024):F1} KB/s";
                                }
                            });

                            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

                            while (true)
                            {
                                var finished = await Task.WhenAny(downloadTask, timeoutTask);

                                if (finished == timeoutTask)
                                    throw new Exception("Timeout");

                                if (downloadTask.IsCompleted)
                                    break;

                                if ((DateTime.Now - lastProgressTime).TotalSeconds > stallSeconds)
                                    throw new Exception("Stalled (no progress)");

                                await Task.Delay(1000);
                            }

                            await downloadTask;
                            await fs.FlushAsync();
                        }

                        var fi = new FileInfo(tempPath);
                        if (!fi.Exists || fi.Length <= 0)
                            throw new Exception("Empty file");

                        if (doc.size > 0 && fi.Length != doc.size)
                            throw new Exception($"Size mismatch {fi.Length}/{doc.size}");

                        using (var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
                        {
                            byte[] header = new byte[16];
                            int read = await fs.ReadAsync(header, 0, header.Length);

                            if (read >= 12)
                            {
                                string headerStr = Encoding.ASCII.GetString(header, 0, read);
                                if (!headerStr.Contains("ftyp"))
                                    throw new Exception("Invalid MP4");
                            }
                        }

                        if (File.Exists(finalPath))
                            File.Delete(finalPath);

                        File.Move(tempPath, finalPath);

                        item.Status = "✅ READY";
                        item.Progress = 100;
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Attempt {attempt}] {ex.Message}");

                        try
                        {
                            if (File.Exists(tempPath))
                                File.Delete(tempPath);
                        }
                        catch { }

                        if (attempt == maxRetry)
                        {
                            item.Status = $"❌ FAILED: {ex.Message}";
                            return;
                        }

                        item.Status = $"⏳ RETRY ({attempt}/{maxRetry}): {ex.Message}";
                        await Task.Delay(TimeSpan.FromSeconds(3 * attempt));
                    }
                }
            }
            finally
            {
                lock (_dedupLock)
                    _processing.Remove(item.FileName);

                FinalizeCrawlItem(item);
            }
        }

        private async Task<Message> RefreshMessageAsync(Message oldMsg)
        {
            var dialogs = await _client.Messages_GetAllDialogs();
            var peer = ResolveFromPeer(oldMsg, dialogs);
            if (peer is null)
                return null;

            int offsetId = 0;
            const int limit = 100;

            for (int page = 0; page < 100; page++)
            {
                var history = await _client.Messages_GetHistory(
                    peer,
                    offset_id: offsetId,
                    add_offset: 0,
                    limit: limit,
                    max_id: 0,
                    min_id: 0,
                    hash: 0);

                var msgs = history.Messages.OfType<Message>().ToList();
                if (msgs.Count == 0)
                    break;

                var found = msgs.FirstOrDefault(m => m.id == oldMsg.id);
                if (found != null)
                    return found;

                if (msgs.Count < limit)
                    break;

                offsetId = msgs[^1].id;
            }

            return null;
        }

        private static InputPeer ResolveFromPeer(Message m, Messages_Dialogs dialogs)
        {
            return m.peer_id switch
            {
                PeerChannel pc when dialogs.chats.TryGetValue(pc.channel_id, out var ch) && ch is Channel ch2
                    => new InputPeerChannel(pc.channel_id, ch2.access_hash),

                PeerChat cc
                    => new InputPeerChat(cc.chat_id),

                PeerUser pu when dialogs.users.TryGetValue(pu.user_id, out var u)
                    => new InputPeerUser(pu.user_id, u.access_hash),

                _ => null
            };
        }

        private void FinalizeCrawlItem(DownloadItem item)
        {
            if (!item.TryFinalize())
                return;
        }

        // ─────────────────────────────────────────────────────────
        //  GROUP NAME
        // ─────────────────────────────────────────────────────────
        private async Task<string> GetGroupNameCached(Message m)
        {
            try
            {
                long id = m.peer_id switch
                {
                    PeerChannel pc => pc.channel_id,
                    PeerChat cc => cc.chat_id,
                    PeerUser pu => pu.user_id,
                    _ => 0
                };

                if (_groupCache.TryGetValue(id, out var cached))
                    return cached;

                var chats = await _client.Messages_GetAllChats();

                string name = m.peer_id switch
                {
                    PeerChannel pc => chats.chats.Values.OfType<Channel>()
                        .FirstOrDefault(x => x.id == pc.channel_id)?.title
                        ?? $"channel_{pc.channel_id}",

                    PeerChat cc => chats.chats.Values.OfType<Chat>()
                        .FirstOrDefault(x => x.id == cc.chat_id)?.title
                        ?? $"group_{cc.chat_id}",

                    _ => "unknown"
                };

                _groupCache[id] = name;
                return name;
            }
            catch
            {
                return "unknown";
            }
        }

        // ─────────────────────────────────────────────────────────
        //  CONFIG
        // ─────────────────────────────────────────────────────────
        private void SaveConfig()
        {
            var cfg = new AppConfig
            {
                ApiId = ApiIdBox.Text,
                ApiHash = ApiHashBox.Text,
                Phone = PhoneBox.Text,
                BotToken = BotTokenBox.Text
            };

            File.WriteAllText(CONFIG_FILE,
                JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
        }

        private void LoadConfig()
        {
            if (!File.Exists(CONFIG_FILE))
                return;

            var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(CONFIG_FILE));
            if (cfg is null)
                return;

            _apiId = cfg.ApiId;
            _apiHash = cfg.ApiHash;
            _phone = cfg.Phone;
            _botToken = cfg.BotToken;

            ApiIdBox.Text = cfg.ApiId;
            ApiHashBox.Text = cfg.ApiHash;
            PhoneBox.Text = cfg.Phone;
            BotTokenBox.Text = cfg.BotToken;
        }

        // ─────────────────────────────────────────────────────────
        //  CRAWL STATE
        // ─────────────────────────────────────────────────────────
        private void SaveCrawlState(CrawlState state)
        {
            try
            {
                File.WriteAllText(CRAWL_STATE_FILE,
                    JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SaveCrawlState] {ex.Message}");
            }
        }

        private CrawlState LoadCrawlState()
        {
            try
            {
                if (!File.Exists(CRAWL_STATE_FILE))
                    return null;

                return JsonSerializer.Deserialize<CrawlState>(File.ReadAllText(CRAWL_STATE_FILE));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadCrawlState] {ex.Message}");
                return null;
            }
        }

        private void LoadCrawlStateToUi()
        {
            var state = LoadCrawlState();
            if (state is null)
            {
                UpdateCrawlUi("Chưa có state crawl.", null, "-", "không có file state");
                return;
            }

            _crawlState = state;

            GroupInputBox.Text = state.Group;

            var selectedDate = state.TargetDate != default
                ? state.TargetDate
                : (state.CurrentDate == default ? DateTime.Today : state.CurrentDate);

            CrawlDatePicker.SelectedDate = selectedDate;

            UpdateCrawlUi(
                state.IsRunning ? "Có phiên crawl đang lưu." : "State đã lưu.",
                state.CurrentDate,
                $"cursor={state.CursorMessageId}",
                $"mốc crawl: {selectedDate:yyyy-MM-dd}");
        }

        private void DeleteCrawlState()
        {
            try
            {
                if (File.Exists(CRAWL_STATE_FILE))
                    File.Delete(CRAWL_STATE_FILE);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeleteCrawlState] {ex.Message}");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  MODELS
    // ─────────────────────────────────────────────────────────────
    public class DownloadItem : INotifyPropertyChanged
    {
        public string FileName { get; set; }
        public string GroupName { get; set; }
        public Message SourceMessage { get; set; }
        public DateTime? CrawlDay { get; set; }

        public long? ApproveChatId { get; set; }
        public int? ApproveMessageId { get; set; }

        private int _finalized;
        public bool TryFinalize() => Interlocked.Exchange(ref _finalized, 1) == 0;

        private string _status;
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        private double _progress;
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class AppConfig
    {
        public string ApiId { get; set; }
        public string ApiHash { get; set; }
        public string Phone { get; set; }
        public string BotToken { get; set; }
    }

    public class CrawlState
    {
        public string Group { get; set; }
        public int CursorMessageId { get; set; }
        public DateTime CurrentDate { get; set; }
        public DateTime TargetDate { get; set; }
        public int EmptyDayStreak { get; set; }
        public bool IsRunning { get; set; }
    }
}
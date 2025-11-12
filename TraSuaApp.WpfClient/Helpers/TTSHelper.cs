using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

public static class TTSHelper
{
    // Hàng đợi các file cần phát
    private static readonly Queue<string> _queue = new();
    private static readonly object _lock = new();

    // Cờ đang phát và player hiện tại
    private static bool _isPlaying = false;
    private static MediaPlayer? _currentPlayer;

    // Lấy Dispatcher UI (của WPF App)
    private static Dispatcher UiDispatcher
        => Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

    // --- NORMALIZE TÊN SẢN PHẨM ---------------------------------
    private static string NormalizeTenSanPham(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input ?? string.Empty;

        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Cf", "Cà Phê" },
            { "Cafe", "Cà Phê" },
            { "TCĐĐ", "Trân Châu Đường Đen" },
            { "TCT", "Trân Châu Trắng" },
            { "TS", "Trà Sữa" },
            { "S/MV", "" },
            { "Olong", "Ô Long" },
        };

        foreach (var kv in replacements)
        {
            // Thay thế theo dạng từ nguyên vẹn, không phân biệt hoa/thường
            input = Regex.Replace(
                input,
                $@"\b{Regex.Escape(kv.Key)}\b",
                kv.Value,
                RegexOptions.IgnoreCase
            );
        }

        return input;
    }

    // Alias tiện gọi: TTSHelper.Speak("xin chào");
    public static Task Speak(string text) => DownloadAndPlayGoogleTTSAsync(text);

    // --- DOWNLOAD + ENQUEUE -------------------------------------
    public static async Task DownloadAndPlayGoogleTTSAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        text = NormalizeTenSanPham(text);

        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio-Files");
        Directory.CreateDirectory(folder);

        string safeFileName = ReplaceInvalidFileNameChars(text);
        if (safeFileName.Length > 50) // tránh tên file quá dài
            safeFileName = safeFileName[..50];

        string filePath = Path.Combine(folder, safeFileName + ".mp3");

        if (!File.Exists(filePath))
        {
            string encodedText = WebUtility.UrlEncode(text);
            string url = $"https://translate.google.com/translate_tts?ie=UTF-8&q={encodedText}&tl=vi&client=tw-ob";

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                byte[] audioBytes = await client.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(filePath, audioBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error downloading audio:\n" + ex.Message,
                                "Download Failed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }
        }

        bool shouldStart = false;

        // Đưa file vào queue, quyết định có cần start phát không
        lock (_lock)
        {
            _queue.Enqueue(filePath);
            if (!_isPlaying)
            {
                _isPlaying = true;
                shouldStart = true;
            }
        }

        if (shouldStart)
        {
            // Bắt đầu phát file đầu tiên trên UI thread
            StartNextOnUi();
        }
    }

    // --- PLAYBACK PIPELINE --------------------------------------
    private static void StartNextOnUi()
    {
        UiDispatcher.BeginInvoke((Action)PlayNextCore, DispatcherPriority.Background);
    }

    private static void PlayNextCore()
    {
        string? nextFile = null;

        lock (_lock)
        {
            if (_queue.Count == 0)
            {
                _isPlaying = false;
                return;
            }

            nextFile = _queue.Dequeue();
        }

        try
        {
            // Dọn player cũ nếu có
            _currentPlayer?.Stop();
            _currentPlayer?.Close();
        }
        catch { }

        // Tạo player mới cho file này
        var player = new MediaPlayer();
        _currentPlayer = player;

        player.Volume = 1.0;

        player.MediaEnded += (s, e) =>
        {
            try
            {
                player.Stop();
                player.Close();
            }
            catch { }

            StartNextOnUi();
        };

        player.MediaFailed += (s, e) =>
        {
            try
            {
                player.Stop();
                player.Close();
            }
            catch { }

            StartNextOnUi();
        };

        try
        {
            player.Open(new Uri(nextFile!, UriKind.Absolute));
            player.Position = TimeSpan.Zero;
            player.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("TTS play error: " + ex.Message);
            // Nếu lỗi thì thử phát file tiếp theo
            StartNextOnUi();
        }
    }

    // --- STOP ----------------------------------------------------
    public static void Stop()
    {
        lock (_lock)
        {
            _queue.Clear();
            _isPlaying = false;
        }

        UiDispatcher.BeginInvoke((Action)(() =>
        {
            try
            {
                _currentPlayer?.Stop();
                _currentPlayer?.Close();
                _currentPlayer = null;
            }
            catch { }
        }));
    }

    // --- UTILS ---------------------------------------------------
    private static string ReplaceInvalidFileNameChars(string input)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            input = input.Replace(c, ' ');
        return input.Trim();
    }
}
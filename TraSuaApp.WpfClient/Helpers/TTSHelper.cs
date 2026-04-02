//using System.IO;
//using System.Net;
//using System.Net.Http;
//using System.Text.RegularExpressions;
//using System.Windows;
//using System.Windows.Media;
//using System.Windows.Threading;











using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

public static class TTSHelper
{
    private static readonly Queue<string> _queue = new();
    private static readonly object _lock = new();

    private static readonly HttpClient _http = new();

    private static bool _isPlaying;
    private static MediaPlayer? _currentPlayer;

    private static Dispatcher UiDispatcher =>
        Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

    private static readonly Dictionary<string, string> _replacements =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "Cf", "Cà Phê" },
            { "Cafe", "Cà Phê" },
            { "TCĐĐ", "Trân Châu Đường Đen" },
            { "TCDĐ", "Trân Châu Đường Đen" },
            { "TCT", "Trân Châu Trắng" },
            { "TS", "Trà Sữa" },
            { "S/MV", "" },
            { "Olong", "Ô Long" },
            { "Ko", "Không" }
        };

    // ================= NORMALIZE =================
    private static string NormalizeTenSanPham(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        input = input
            .Replace("#", ",")
            .Replace("+", " thêm ");

        foreach (var kv in _replacements)
        {
            input = Regex.Replace(
                input,
                $@"\b{Regex.Escape(kv.Key)}\b",
                kv.Value,
                RegexOptions.IgnoreCase);
        }

        input = Regex.Replace(input, @"\s+", " ");
        input = Regex.Replace(input, @"\s*,\s*", ", ");

        return input.Trim();
    }

    // ================= PUBLIC =================
    public static Task Speak(string text) =>
        DownloadAndPlayGoogleTTSAsync(text);

    public static async Task DownloadAndPlayGoogleTTSAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        text = NormalizeTenSanPham(text);

        string folder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Audio-Files");

        Directory.CreateDirectory(folder);

        string safeName = ReplaceInvalidFileNameChars(text);

        if (safeName.Length > 250)
            safeName = safeName[..250];

        string filePath = Path.Combine(folder, safeName + ".mp3");

        if (!File.Exists(filePath))
        {
            try
            {
                await DownloadAudio(text, filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error downloading audio:\n" + ex.Message,
                    "Download Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
        }

        Enqueue(filePath);
    }

    // ================= DOWNLOAD =================
    private static async Task DownloadAudio(string text, string filePath)
    {
        string encoded = WebUtility.UrlEncode(text);

        string url =
            $"https://translate.google.com/translate_tts?ie=UTF-8&q={encoded}&tl=vi&client=tw-ob";

        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        byte[] audio = await _http.GetByteArrayAsync(url);

        await File.WriteAllBytesAsync(filePath, audio);
    }

    // ================= QUEUE =================
    private static void Enqueue(string file)
    {
        bool start = false;

        lock (_lock)
        {
            _queue.Enqueue(file);

            if (!_isPlaying)
            {
                _isPlaying = true;
                start = true;
            }
        }

        if (start)
            StartNextOnUi();
    }

    private static void StartNextOnUi()
    {
        UiDispatcher.BeginInvoke(
            (Action)PlayNextCore,
            DispatcherPriority.Background);
    }

    // ================= PLAYER =================
    private static void PlayNextCore()
    {
        string? file;

        lock (_lock)
        {
            if (_queue.Count == 0)
            {
                _isPlaying = false;
                return;
            }

            file = _queue.Dequeue();
        }

        try
        {
            _currentPlayer?.Stop();
            _currentPlayer?.Close();
        }
        catch { }

        var player = new MediaPlayer();
        _currentPlayer = player;

        player.Volume = 1.0;

        player.MediaEnded += (_, _) => FinishPlayer(player);
        player.MediaFailed += (_, _) => FinishPlayer(player);

        try
        {
            player.Open(new Uri(file!, UriKind.Absolute));
            player.Position = TimeSpan.Zero;
            player.Play();
        }
        catch
        {
            StartNextOnUi();
        }
    }

    private static void FinishPlayer(MediaPlayer player)
    {
        try
        {
            player.Stop();
            player.Close();
        }
        catch { }

        StartNextOnUi();
    }

    // ================= STOP =================
    public static void Stop()
    {
        lock (_lock)
        {
            _queue.Clear();
            _isPlaying = false;
        }

        UiDispatcher.BeginInvoke(() =>
        {
            try
            {
                _currentPlayer?.Stop();
                _currentPlayer?.Close();
                _currentPlayer = null;
            }
            catch { }
        });
    }

    // ================= UTILS =================
    private static string ReplaceInvalidFileNameChars(string input)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            input = input.Replace(c, ' ');

        return input.Trim();
    }
}
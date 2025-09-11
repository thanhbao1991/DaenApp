using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

public static class TTSHelper
{
    private static readonly MediaPlayer _player = new MediaPlayer();
    private static readonly Queue<string> _queue = new Queue<string>();
    private static bool _isPlaying = false;
    private static readonly object _lock = new object();

    static TTSHelper()
    {
        _player.MediaEnded += Player_MediaEnded;
        _player.MediaFailed += Player_MediaFailed;
    }
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


    public static async Task DownloadAndPlayGoogleTTSAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        text = NormalizeTenSanPham(text);

        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio-Files");
        Directory.CreateDirectory(folder);

        string safeFileName = ReplaceInvalidFileNameChars(text);
        if (safeFileName.Length > 50) // tránh tên file quá dài
            safeFileName = safeFileName.Substring(0, 50);

        string filePath = Path.Combine(folder, safeFileName + ".mp3");

        if (!File.Exists(filePath))
        {
            string encodedText = WebUtility.UrlEncode(text);
            string url = $"https://translate.google.com/translate_tts?ie=UTF-8&q={encodedText}&tl=vi&client=tw-ob";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                    byte[] audioBytes = await client.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(filePath, audioBytes);
                }
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

        lock (_lock)
        {
            _queue.Enqueue(filePath);
            if (!_isPlaying)
                PlayNext();
        }
    }

    private static void PlayNext()
    {
        if (_queue.Count == 0)
        {
            _isPlaying = false;
            return;
        }

        _isPlaying = true;
        string nextFile = _queue.Dequeue();

        try
        {
            _player.Open(new Uri(nextFile));
            _player.Volume = 1.0; // max volume
            _player.Play();
        }
        catch
        {
            // nếu lỗi thì thử phát cái tiếp theo
            _isPlaying = false;
            PlayNext();
        }
    }

    private static void Player_MediaEnded(object? sender, EventArgs e)
    {
        _isPlaying = false;
        PlayNext();
    }

    private static void Player_MediaFailed(object? sender, ExceptionEventArgs e)
    {
        _isPlaying = false;
        PlayNext();
    }
    public static void Stop()
    {
        try
        {
            _player.Stop();
            _queue.Clear();
            _isPlaying = false;
        }
        catch { }
    }
    private static string ReplaceInvalidFileNameChars(string input)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            input = input.Replace(c, ' ');
        return input.Trim();
    }
}
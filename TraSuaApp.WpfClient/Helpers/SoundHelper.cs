using System.IO;
using System.Windows.Media;

namespace TraSuaApp.WpfClient.Helpers
{
    public static class AudioHelper
    {
        private static MediaPlayer? _mediaPlayer;

        /// <summary>
        /// Phát file âm thanh 1 lần
        /// </summary>
        public static void Play(string fileName)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(baseDir, "Resources", fileName);

                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Không tìm thấy file âm thanh: {filePath}");
                    return;
                }

                if (_mediaPlayer == null)
                    _mediaPlayer = new MediaPlayer();

                _mediaPlayer.Stop(); // reset trước
                _mediaPlayer.Open(new Uri(filePath, UriKind.Absolute));
                _mediaPlayer.Play();

                System.Diagnostics.Debug.WriteLine($"🟟 Đang phát file âm thanh: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ Lỗi phát âm thanh: " + ex.Message);
            }
        }

        /// <summary>
        /// Phát lặp lại file âm thanh cho đến khi gọi Stop()
        /// </summary>
        public static void PlayLoop(string fileName)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(baseDir, "Resources", fileName);

                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Không tìm thấy file âm thanh: {filePath}");
                    return;
                }

                if (_mediaPlayer == null)
                    _mediaPlayer = new MediaPlayer();

                _mediaPlayer.Stop(); // reset trước
                _mediaPlayer.Open(new Uri(filePath, UriKind.Absolute));
                _mediaPlayer.MediaEnded -= OnMediaEnded; // tránh gắn nhiều lần
                _mediaPlayer.MediaEnded += OnMediaEnded;
                _mediaPlayer.Play();

                System.Diagnostics.Debug.WriteLine($"🟟 Đang phát lặp file âm thanh: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ Lỗi phát âm thanh loop: " + ex.Message);
            }
        }

        /// <summary>
        /// Dừng phát âm thanh
        /// </summary>
        public static void Stop()
        {
            try
            {
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.MediaEnded -= OnMediaEnded;
                    System.Diagnostics.Debug.WriteLine("🟟 Đã dừng phát âm thanh");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ Lỗi khi dừng âm thanh: " + ex.Message);
            }
        }

        private static void OnMediaEnded(object? sender, EventArgs e)
        {
            if (_mediaPlayer == null) return;
            _mediaPlayer.Position = TimeSpan.Zero;
            _mediaPlayer.Play();
        }
    }
}
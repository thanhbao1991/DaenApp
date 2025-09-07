using System.IO;
using System.Windows.Media;

namespace TraSuaApp.WpfClient.Helpers
{
    public static class AudioHelper
    {
        private static MediaPlayer? _mediaPlayer;

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

                _mediaPlayer.Open(new Uri(filePath, UriKind.Absolute));
                _mediaPlayer.Play();

                System.Diagnostics.Debug.WriteLine($"🟟 Đang phát file âm thanh: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ Lỗi phát âm thanh: " + ex.Message);
            }
        }
    }


}
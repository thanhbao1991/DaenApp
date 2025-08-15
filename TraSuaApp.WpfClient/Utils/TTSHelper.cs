using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;

public class TTSHelper
{
    public static async Task DownloadAndPlayGoogleTTSAsync(string text)
    {
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio-Files");
        Directory.CreateDirectory(folder);

        // Replace invalid filename characters with space
        string safeFileName = ReplaceInvalidFileNameChars(text);
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
                    await Task.Run(() => File.WriteAllBytes(filePath, audioBytes));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error downloading audio:\n" + ex.Message, "Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        // Play the mp3 using WPF MediaPlayer
        try
        {
            var player = new System.Windows.Media.MediaPlayer();
            player.Open(new Uri(filePath));
            player.Play();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error playing audio:\n" + ex.Message, "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string ReplaceInvalidFileNameChars(string input)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(c, ' ');
        }
        return input.Trim();
    }
}
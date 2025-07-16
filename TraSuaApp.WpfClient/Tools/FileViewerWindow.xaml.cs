using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TraSuaApp.WpfClient.Views
{
    public partial class FileViewerWindow : Window
    {
        private List<FileInfo> allFiles = new();

        private readonly string webhookUrl = "https://discord.com/api/webhooks/1385632148387533011/MmRNpkKCoslZwNO2F9uJd_ZCjiaSvXMKeIpQlDP7gpDBwk1HZt1g2nonmEUiOVITaK0H";

        public FileViewerWindow()
        {
            InitializeComponent();
            LoadFiles();
        }

        private void LoadFiles()
        {
            string projectRoot = @"D:\New folder";
            string[] extensions = [".cs", ".xaml"];

            if (!Directory.Exists(projectRoot))
            {
                MessageBox.Show("Không tìm thấy thư mục: " + projectRoot);
                return;
            }

            allFiles = Directory.GetFiles(projectRoot, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    string ext = Path.GetExtension(f);
                    string fileName = Path.GetFileName(f).ToLower();

                    if (!extensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                        return false;

                    if (fileName.EndsWith(".g.cs") ||
                        fileName.EndsWith(".g.i.cs") ||
                        fileName.Contains("copy") ||
                        fileName.Contains("_"))
                        return false;

                    return true;
                })
                .Select(f => new FileInfo(f))
                .ToList();

            lstFiles.ItemsSource = allFiles;
        }

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = txtFilter.Text.Trim().ToLower();
            string keyword2 = txtFilter2.Text.Trim().ToLower();

            lstFiles.ItemsSource = allFiles
                .Where(f =>
                f.Name.ToLower().Contains(keyword)
                ||
                f.Name.ToLower().Contains(keyword2)

                )
                .ToList();
        }

        private async void btnCopyAndSend_Click(object sender, RoutedEventArgs e)
        {
            if (txt.Text.Length > 0)
            {
                await SendToDiscordSmart(txt.Text);
                txt.Text = "";
            }
            else
            {
                var selected = lstFiles.SelectedItems.Cast<FileInfo>().ToList();

                if (selected.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một file.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var combined = string.Join("\n\n", selected.Select(f =>
                    {
                        string content = File.ReadAllText(f.FullName);
                        return $"----- {f.Name} -----\n{content}";
                    }));
                    //Clipboard.SetText(combined);

                    // ✅ Gửi lên Discord
                    await SendToDiscordSmart(combined);

                    //MessageBox.Show("✅ Đã copy vào clipboard và gửi lên Discord.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ Lỗi khi thực hiện: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async Task SendToDiscordSmart(string message)
        {
            using HttpClient client = new();

            if (message.Length < 1900)
            {
                var payload = new { content = message };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(webhookUrl, content);
            }
            else
            {
                var fileBytes = Encoding.UTF8.GetBytes(message);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

                using var form = new MultipartFormDataContent();
                form.Add(fileContent, "file", "code.txt");

                await client.PostAsync(webhookUrl, form);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
            {
                // Hành động khi Ctrl + C được nhấn
                btnCopyAndSend_Click(null!, null!);
                e.Handled = true; // Ngăn không cho hệ thống xử lý thêm
            }
        }
    }
}
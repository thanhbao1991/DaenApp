using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace TraSuaApp.WpfClient.Tools
{
    public partial class FileViewerWindow : Window
    {
        private List<FileInfo> allFiles = new();

        public FileViewerWindow()
        {
            InitializeComponent();
            LoadFiles();
        }

        private void LoadFiles()
        {
            string projectRoot = @"D:\New folder";
            if (!Directory.Exists(projectRoot))
            {
                MessageBox.Show("Thư mục không tồn tại.");
                return;
            }

            var files = Directory.GetFiles(projectRoot, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.Contains(".g.i.cs"))
                .Where(f => !f.Contains(".g.cs"))
                .Where(f => !f.Contains("2025"))
                .Where(f => f.EndsWith(".cs")
                || f.EndsWith(".xaml")
                || f.EndsWith(".html")

                )
                .Select(f => new FileInfo(f))
                .ToList();

            allFiles = files;
            FilterFiles();
        }

        private void FilterFiles()
        {
            var keywords = txtSearch.Text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.ToLowerInvariant())
                .ToList();

            var filtered = allFiles
                .Where(f => keywords.Count == 0 || keywords.Any(k => f.Name.ToLowerInvariant().Contains(k)))
                .ToList();

            lstFiles.ItemsSource = filtered;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterFiles();
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            lstFiles.SelectAll();
            //LoadFiles();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnCloneWithReplace_Click(object sender, RoutedEventArgs e)
        {
            var selected = lstFiles.SelectedItems.Cast<FileInfo>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một file.");
                return;
            }

            string fromText = txtFrom.Text.Trim();
            string toText = txtTo.Text.Trim();

            if (string.IsNullOrWhiteSpace(fromText) || string.IsNullOrWhiteSpace(toText))
            {
                MessageBox.Show("Vui lòng nhập cả 'Từ gốc' và 'Thay bằng'.");
                return;
            }

            foreach (var file in selected)
            {
                string oldContent = File.ReadAllText(file.FullName);
                string oldName = file.Name;

                string newFileName = oldName.Replace(fromText, toText);
                string newContent = oldContent.Replace(fromText, toText);
                string newPath = Path.Combine(file.DirectoryName!, newFileName);

                File.WriteAllText(newPath, newContent, Encoding.UTF8);
            }

            MessageBox.Show("✅ Đã tạo bản sao với tên mới!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void btnSendToDiscord_Click(object sender, RoutedEventArgs e)
        {
            var selected = lstFiles.SelectedItems.Cast<FileInfo>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Chọn ít nhất 1 file để gửi.");
                return;
            }

            string webhookUrl = "https://discord.com/api/webhooks/1415629639992217670/RUDcNljSV_thvgiX0eCftW3E7e6u_8pDtHhULWtbvHlZSwLI39NzayQul9XHFDMNbgFA";
            string combined = "";
            foreach (var file in selected)
            {
                string content = File.ReadAllText(file.FullName);
                string fileName = file.Name;

                combined += $"🟟 `{fileName}`\n";
                combined += "```csharp\n" + content.Trim() + "\n```\n\n";
            }

            using var client = new HttpClient();

            if (combined.Length < 1900)
            {
                var payload = new
                {
                    content = combined
                };

                var json = JsonSerializer.Serialize(payload);
                var response = await client.PostAsync(webhookUrl,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                    MessageBox.Show("❌ Gửi thất bại!");
            }
            else
            {
                var contentToSend = new MultipartFormDataContent();
                var bytes = Encoding.UTF8.GetBytes(combined);
                var fileContent = new ByteArrayContent(bytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                contentToSend.Add(fileContent, "file", "AllFiles.txt");

                var response = await client.PostAsync(webhookUrl, contentToSend);

                if (!response.IsSuccessStatusCode)
                    MessageBox.Show("❌ Gửi file thất bại!");
            }

            //MessageBox.Show("✅ Đã gửi toàn bộ nội dung lên Discord!");
        }
    }
}

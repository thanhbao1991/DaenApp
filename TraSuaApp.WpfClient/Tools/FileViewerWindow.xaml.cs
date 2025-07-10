using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace TraSuaApp.WpfClient.Views
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

                    // Lọc theo phần mở rộng
                    if (!extensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                        return false;

                    // Bỏ qua các file dạng *.g.cs, *.g.i.cs hoặc có chữ "copy"
                    if (fileName.EndsWith(".g.cs") ||
                    fileName.EndsWith(".g.i.cs") ||
                    fileName.ToLower().Contains("copy") ||
                    fileName.ToLower().Contains("_")

                    )
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
            lstFiles.ItemsSource = allFiles
                .Where(f => f.Name.ToLower().Contains(keyword))
                .ToList();
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
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

                Clipboard.SetText(combined);
                MessageBox.Show("✅ Nội dung đã được copy vào clipboard!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace TraSuaApp.WpfClient.Views
{
    public partial class CodeGeneratorWindow : Window
    {
        private string _entityName = "";
        private CodeGenService? _service;

        public CodeGeneratorWindow()
        {
            InitializeComponent();
        }

        private void ChooseFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "C# files (*.cs)|*.cs",
                Title = "Chọn file Entity"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = dialog.FileName;
            }
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePath = FilePathTextBox.Text;
                if (!File.Exists(filePath))
                {
                    StatusTextBlock.Text = "❌ File không tồn tại.";
                    return;
                }

                _entityName = Path.GetFileNameWithoutExtension(filePath);
                var entityNamespace = "TraSuaApp.Domain.Entities";

                _service = new CodeGenService
                {
                    EntityName = _entityName,
                    EntityNamespace = entityNamespace
                };


                PreviewTextBox1.Text = _service.GenerateAutoMapperProfileEntry();
                PreviewTextBox2.Text = _service.GenerateAddScopedService();

                StatusTextBlock.Text = $"✅ Đã sinh code cho: {_entityName} (xem trước phía dưới)";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "❌ Lỗi khi sinh mã: " + ex.Message;
            }





        }
        private string? FindSolutionDirectory()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                var slnFiles = Directory.GetFiles(dir, "*.sln");
                if (slnFiles.Length > 0)
                    return dir;

                dir = Directory.GetParent(dir)?.FullName;
            }
            return null;
        }
        private void SaveToFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_service == null || string.IsNullOrWhiteSpace(_entityName))
            {
                StatusTextBlock.Text = "❌ Vui lòng preview trước khi lưu.";
                return;
            }

            try
            {
                // ✅ Tìm thư mục chứa file .sln
                string? slnDir = FindSolutionDirectory();
                if (slnDir == null)
                {
                    StatusTextBlock.Text = "❌ Không tìm thấy file .sln.";
                    return;
                }

                // ✅ Xác định đường dẫn từng dự án con
                string appDir = Path.Combine(slnDir, "TraSuaApp.Application", "Interfaces");
                string infraDir = Path.Combine(slnDir, "TraSuaApp.Infrastructure", "Services");
                string apiDir = Path.Combine(slnDir, "TraSuaApp.Api", "Controllers");
                string sharedDir = Path.Combine(slnDir, "TraSuaApp.Shared", "Dtos");

                Directory.CreateDirectory(appDir);
                Directory.CreateDirectory(infraDir);
                Directory.CreateDirectory(apiDir);
                Directory.CreateDirectory(sharedDir);

                // ✅ Lưu các file tương ứng
                File.WriteAllText(
                    Path.Combine(appDir, $"I{_entityName}Service.cs"),
                    _service.GenerateServiceInterface());

                File.WriteAllText(
                    Path.Combine(infraDir, $"{_entityName}Service.cs"),
                    _service.GenerateServiceImplementation());

                File.WriteAllText(
                    Path.Combine(apiDir, $"{_entityName}Controller.cs"),
                    _service.GenerateController());

                File.WriteAllText(
                    Path.Combine(sharedDir, $"{_entityName}Dto.cs"),
                    _service.GenerateDto());

                // ❌ Không lưu AutoMapper Entry
                StatusTextBlock.Text = $"✅ Đã lưu mã vào đúng thư mục dự án.";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "❌ Lỗi khi lưu file: " + ex.Message;
            }
        }

        private void PreviewTextBox1_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).SelectAll();
            Clipboard.SetText((sender as TextBox).Text);
        }
    }
}


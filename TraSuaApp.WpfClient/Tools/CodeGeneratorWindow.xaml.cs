using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace TraSuaApp.WpfClient.Tools
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

                // ✅ Chỉ preview, chưa tạo file
                var preview = $"""
                    // ========== I{_entityName}Service.cs ==========
                    {_service.GenerateServiceInterface()}


                    // ========== {_entityName}Service.cs ==========
                    {_service.GenerateServiceImplementation()}


                    // ========== {_entityName}Controller.cs ==========
                    {_service.GenerateController()}


                    // ========== {_entityName}Dto.cs ==========
                    {_service.GenerateDto()}


                    // ========== AutoMapper Entry ==========
                    {_service.GenerateAutoMapperProfileEntry()}
                    """;

                PreviewTextBox.Text = preview;
                StatusTextBlock.Text = $"✅ Đã sinh code cho: {_entityName} (xem trước phía dưới)";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "❌ Lỗi khi sinh mã: " + ex.Message;
            }
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
                var outputRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", _entityName);
                Directory.CreateDirectory(outputRoot);

                File.WriteAllText(Path.Combine(outputRoot, $"I{_entityName}Service.cs"), _service.GenerateServiceInterface());
                File.WriteAllText(Path.Combine(outputRoot, $"{_entityName}Service.cs"), _service.GenerateServiceImplementation());
                File.WriteAllText(Path.Combine(outputRoot, $"{_entityName}Controller.cs"), _service.GenerateController());
                File.WriteAllText(Path.Combine(outputRoot, $"{_entityName}Dto.cs"), _service.GenerateDto());
                File.WriteAllText(Path.Combine(outputRoot, $"AutoMapperEntry_{_entityName}.txt"), _service.GenerateAutoMapperProfileEntry());

                StatusTextBlock.Text = $"✅ Đã lưu mã vào thư mục /Output/{_entityName}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "❌ Lỗi khi lưu file: " + ex.Message;
            }
        }
    }
}
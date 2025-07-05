using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace TraSuaApp.WpfClient.Tools
{
    public partial class CodeGeneratorWindow : Window
    {
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
            var filePath = FilePathTextBox.Text;
            if (!File.Exists(filePath))
            {
                StatusTextBlock.Text = "❌ File không tồn tại.";
                return;
            }

            var entityName = Path.GetFileNameWithoutExtension(filePath);
            var entityNamespace = "TraSuaApp.Domain.Entities";

            var outputRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", entityName);
            Directory.CreateDirectory(outputRoot);

            var service = new CodeGenService
            {
                EntityName = entityName,
                EntityNamespace = entityNamespace
            };

            File.WriteAllText(Path.Combine(outputRoot, $"I{entityName}Service.cs"), service.GenerateServiceInterface());
            File.WriteAllText(Path.Combine(outputRoot, $"{entityName}Service.cs"), service.GenerateServiceImplementation());
            File.WriteAllText(Path.Combine(outputRoot, $"{entityName}Controller.cs"), service.GenerateController());
            File.WriteAllText(Path.Combine(outputRoot, $"{entityName}Dto.cs"), service.GenerateDto());
            File.WriteAllText(Path.Combine(outputRoot, $"AutoMapperEntry_{entityName}.txt"), service.GenerateAutoMapperProfileEntry());

            StatusTextBlock.Text = $"✅ Đã sinh code cho: {entityName} tại thư mục /Output/{entityName}";
        }
    }
}
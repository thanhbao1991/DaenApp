using System.Text.Json;
using System.Windows;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.WpfClient.Views
{
    public partial class LogDetailWindow : Window
    {
        public string Header { get; set; }
        public List<LogEntry> Entries { get; set; } = new();

        public LogDetailWindow(LogDto log)
        {
            InitializeComponent();
            DataContext = this;

            // Tiêu đề
            Header = $"Người dùng: {log.UserName}  |  Thao tác: {log.Method} {log.Path}";

            // Parse raw JSON (đã loại bỏ marker nếu cần)
            var raw = log.ResponseBodyShort ?? "";
            const string trunc = "... [truncated]";
            if (raw.EndsWith(trunc)) raw = raw[..^trunc.Length];

            JsonDocument doc;
            try { doc = JsonDocument.Parse(raw); }
            catch
            {
                // không parse được, hiển thị nguyên raw rồi thoát
                Entries.Add(new LogEntry { Property = "<Invalid JSON>", Before = raw, After = "" });
                return;
            }

            var root = doc.RootElement;

            // Lấy beforeData và afterData
            bool hasBefore = root.TryGetProperty("beforeData", out var beforeData);
            bool hasAfter = root.TryGetProperty("afterData", out var afterData);

            // Nếu thêm mới
            if ((!hasBefore || beforeData.ValueKind == JsonValueKind.Null)
                && hasAfter && afterData.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in afterData.EnumerateObject())
                    Entries.Add(new LogEntry
                    {
                        Property = p.Name,
                        Before = "",
                        After = p.Value.ToString()
                    });
            }
            // Nếu xóa
            else if (hasBefore && (!hasAfter || afterData.ValueKind == JsonValueKind.Null))
            {
                foreach (var p in beforeData.EnumerateObject())
                    Entries.Add(new LogEntry
                    {
                        Property = p.Name,
                        Before = p.Value.ToString(),
                        After = ""
                    });
            }
            // Nếu cập nhật
            else if (hasBefore && hasAfter
                && beforeData.ValueKind == JsonValueKind.Object
                && afterData.ValueKind == JsonValueKind.Object)
            {
                var beforeDict = beforeData.EnumerateObject()
                                           .ToDictionary(x => x.Name, x => x.Value);
                foreach (var p in afterData.EnumerateObject())
                {
                    beforeDict.TryGetValue(p.Name, out var b);
                    var a = p.Value;
                    if (!JsonElement.DeepEquals(b, a))
                    {
                        Entries.Add(new LogEntry
                        {
                            Property = p.Name,
                            Before = b.ToString(),
                            After = a.ToString()
                        });
                    }
                }
                if (Entries.Count == 0)
                    Entries.Add(new LogEntry { Property = "(Không có thay đổi)", Before = "", After = "" });
            }
            else
            {
                Entries.Add(new LogEntry { Property = "(Không xác định)", Before = "", After = "" });
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class LogEntry
    {
        public string Property { get; set; } = "";
        public string Before { get; set; } = "";
        public string After { get; set; } = "";
    }
}

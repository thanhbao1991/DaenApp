using System.IO;
using System.Text.Json;

namespace TraSuaApp.WpfClient.GptOrders
{
    public class QuickOrderItem
    {
        public int Line { get; set; } = 1;          // 1-based theo LINES
        public Guid Id { get; set; } = Guid.Empty;  // Id sản phẩm trong MENU
        public int SoLuong { get; set; } = 1;
        public string NoteText { get; set; } = "";
    }

    public class QuickOrderMemory
    {
        public List<string> Fillers { get; set; } = new();        // xưng hô/cảm thán thường gặp (chị, em, ơi, hỷ...)
        public List<string> ShortcutBias { get; set; } = new();   // gõ tắt phổ biến (“cf”, “cafe”, “phe”, “tcdd”...)
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public static string DefaultPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "TraSuaApp", "QuickOrder.memory.json");

        public static QuickOrderMemory LoadOrCreate(string? path = null)
        {
            path ??= DefaultPath;
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var obj = JsonSerializer.Deserialize<QuickOrderMemory>(json);
                    if (obj != null) return obj;
                }
            }
            catch { /* ignore */ }

            // seed mặc định
            return new QuickOrderMemory
            {
                Fillers = new()
                {
                    "chi","anh","em","ban","oi","nha","nhe","ha","hen","di","ne","nhe nha","nhe nhe","nha a","nha e","nhe a","nhe e",
                    "hy","hi","hí","hỷ","haiz"
                },
                ShortcutBias = new()
                {
                    "cf","cafe","phe","st","ts","tcdd","tcđđ","tcdđ","nc","sto","oolong","olong"
                }
            };
        }

        public static void Save(QuickOrderMemory mem, string? path = null)
        {
            path ??= DefaultPath;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var json = JsonSerializer.Serialize(mem, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { /* ignore */ }
        }
    }
}
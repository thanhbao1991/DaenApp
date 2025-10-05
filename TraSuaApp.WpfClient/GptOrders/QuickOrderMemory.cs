using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TraSuaApp.WpfClient.Ordering
{
    /// <summary>
    /// Bộ nhớ tự học đơn giản: lưu các dòng LINES bị miss.
    /// - File: %AppData%\TraSuaApp\QuickOrder.memory.json
    /// - Chỉ ghi khi có miss/correction ⇒ tự tạo file/thư mục lần đầu.
    /// - Chỉ báo Discord khi phát hiện miss MỚI (chưa từng thấy).
    /// </summary>
    public sealed class QuickOrderMemory
    {
        private static readonly object _lock = new();
        private static QuickOrderMemory? _instance;

        public static QuickOrderMemory Instance => _instance ??= Load();

        public int Version { get; set; } = 1;
        public HashSet<string> MissLines { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public int Runs { get; set; }
        public int Misses { get; set; }
        public int Corrections { get; set; }

        private static string RootFolder
        {
            get
            {
                var root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TraSuaApp");
                Directory.CreateDirectory(root);
                return root;
            }
        }

        private static string MemoryPath => Path.Combine(RootFolder, "QuickOrder.memory.json");

        private static QuickOrderMemory Load()
        {
            try
            {
                if (File.Exists(MemoryPath))
                {
                    var json = File.ReadAllText(MemoryPath, Encoding.UTF8);
                    var mem = JsonSerializer.Deserialize<QuickOrderMemory>(json);
                    if (mem != null) return mem;
                }
            }
            catch { /* ignore & start fresh */ }
            return new QuickOrderMemory();
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(MemoryPath, json, Encoding.UTF8);
            }
            catch { /* tránh crash app vì lỗi I/O */ }
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var t = s.ToLowerInvariant().Trim();
            t = Regex.Replace(t, @"\s+", " ");
            return t;
        }

        /// <summary>
        /// Ghi nhận một dòng bị miss. Trả về true nếu là MISS MỚI (chưa từng gặp).
        /// Đồng thời tăng stats và lưu file.
        /// </summary>
        public bool MarkMiss(string line)
        {
            lock (_lock)
            {
                Runs++;
                Misses++;
                var key = Normalize(line);
                var isNew = MissLines.Add(key);
                Save();
                return isNew;
            }
        }

        public void MarkCorrection()
        {
            lock (_lock)
            {
                Runs++;
                Corrections++;
                Save();
            }
        }
    }
}
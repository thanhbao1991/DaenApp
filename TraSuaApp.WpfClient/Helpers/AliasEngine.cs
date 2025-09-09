//using System.IO;
//using System.Text.Json;

//namespace TraSuaApp.WpfClient.Services
//{
//    public static class AliasEngine
//    {
//        private static readonly Dictionary<string, int> _observed = new();

//        public static string TryResolveOrLearn(string userLine, IEnumerable<string> menu, AliasStore store)
//        {
//            const double AUTO_THRESHOLD = 0.93;      // đủ chắc để lưu alias
//            const double EPHEMERAL_THRESHOLD = 0.88; // đủ giống để dùng ngay lần đầu
//            const int MIN_COUNT_FOR_AUTO = 2;        // gặp ≥2 lần mới lưu

//            var key = TextUtil.ToNoAccent(userLine);
//            if (string.IsNullOrWhiteSpace(key)) return userLine;

//            // 1) Alias đã biết
//            var mapped = store.Resolve(userLine, menu);
//            if (!string.IsNullOrEmpty(mapped)) return mapped;

//            // 2) Tìm tên MENU gần nhất
//            string? best = null;
//            double bestScore = 0;
//            foreach (var m in menu)
//            {
//                var score = JaroWinkler(key, TextUtil.ToNoAccent(m));
//                if (score > bestScore) { bestScore = score; best = m; }
//            }

//            // 2a) Nếu rất giống → dùng ngay (ép map tạm thời)
//            if (best != null && bestScore >= EPHEMERAL_THRESHOLD)
//            {
//                _observed.TryGetValue(key, out var c);
//                _observed[key] = c + 1;

//                // 2b) Nếu đủ chắc và gặp nhiều lần → lưu alias xuống ổ đĩa
//                if (bestScore >= AUTO_THRESHOLD && _observed[key] >= MIN_COUNT_FOR_AUTO)
//                {
//                    store.AddOrUpdate(userLine, best);
//                    store.Save();
//                }
//                return best;
//            }

//            // 3) Không đủ chắc → giữ nguyên để GPT thử
//            _observed.TryGetValue(key, out var c2);
//            _observed[key] = c2 + 1;
//            return userLine;
//        }

//        // 🟟 TextUtil & JaroWinkler bạn đã có sẵn trong code cũ, giữ nguyên
//        private static double JaroWinkler(string s1, string s2)
//        {
//            // ... (giữ nguyên như code cũ của bạn)
//            return 0.0;
//        }
//    }

//    public class AliasStore
//    {
//        private readonly string _file;
//        private readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);

//        public AliasStore(string file = "alias.json")
//        {
//            _file = file;
//            if (File.Exists(file))
//            {
//                var json = File.ReadAllText(file);
//                _map = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
//                        ?? new(StringComparer.OrdinalIgnoreCase);
//            }
//        }

//        public string Resolve(string key, IEnumerable<string> menu)
//        {
//            if (_map.TryGetValue(TextUtil.ToNoAccent(key), out var v))
//            {
//                // chỉ trả về nếu còn tồn tại trong menu
//                if (menu.Contains(v, StringComparer.OrdinalIgnoreCase))
//                    return v;
//            }
//            return "";
//        }

//        public void AddOrUpdate(string alias, string menuName)
//        {
//            _map[TextUtil.ToNoAccent(alias)] = menuName;
//        }

//        public void Save()
//        {
//            File.WriteAllText(_file,
//                JsonSerializer.Serialize(_map, new JsonSerializerOptions { WriteIndented = true }));
//        }
//    }
//}
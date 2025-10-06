using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TraSuaApp.Shared.Config;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.AiOrdering
{
    /// <summary>
    /// Store local (JSON) để "học" mapping dòng nhập → sản phẩm.
    /// Hỗ trợ học CHUNG (CustomerId=null) và THEO KHÁCH (CustomerId=guid) + recency decay.
    /// Cung cấp SHORTLIST để nhét vào prompt GPT.
    /// </summary>
    public class QuickGptLearningStore
    {
        private static readonly Lazy<QuickGptLearningStore> _lazy =
            new(() => new QuickGptLearningStore(Config.apiChatGptKey));
        public static QuickGptLearningStore Instance => _lazy.Value;

        private readonly string _apiKey;
        private readonly string _storePath;
        private readonly SemaphoreSlim _mu = new(1, 1);

        private class Entry
        {
            public string LineText { get; set; } = "";      // raw line
            public string NormText { get; set; } = "";      // đã chuẩn hoá
            public Guid ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public Guid? CustomerId { get; set; }           // null = học chung
            public int Count { get; set; } = 0;
            public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
            public DateTime LastSeen { get; set; } = DateTime.UtcNow;
            public float[]? Embedding { get; set; }         // optional
        }

        private class StoreModel
        {
            public List<Entry> Items { get; set; } = new();
            public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;
            public int Version { get; set; } = 2; // v2: thêm NormText + CustomerId
        }

        private StoreModel _model = new();

        private QuickGptLearningStore(string apiKey)
        {
            _apiKey = apiKey;

            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TraSuaApp", "AiOrdering");
            Directory.CreateDirectory(baseDir);
            _storePath = Path.Combine(baseDir, "gpt-learning-store.json");

            try { _ = LoadAsync(); } catch { /* ignore */ } // khởi chạy async, KHÔNG block UI
        }

        // ================== PUBLIC: LEARN ==================

        /// <summary>Học 1 cặp (line → sản phẩm) sau khi lưu hóa đơn thành công.</summary>
        public async Task LearnAsync(Guid? customerId, string lineText, Guid productId, string productName)
        {
            var norm = Normalize(lineText);

            await _mu.WaitAsync();
            try
            {
                var e = _model.Items.FirstOrDefault(x =>
                    x.CustomerId == customerId &&
                    x.ProductId == productId &&
                    x.NormText == norm);

                if (e == null)
                {
                    e = new Entry
                    {
                        CustomerId = customerId,
                        LineText = StringHelper.NormalizeText(lineText),
                        NormText = StringHelper.NormalizeText(norm),
                        ProductId = productId,
                        ProductName = StringHelper.NormalizeText(productName),
                        Count = 0,
                        FirstSeen = DateTime.Now
                    };
                    _model.Items.Add(e);
                }

                e.Count += 1;
                e.LastSeen = DateTime.UtcNow;

                try { e.Embedding = await GetOrCreateEmbeddingAsync(norm); } catch { /* optional */ }

                await SaveAsync();
            }
            finally { _mu.Release(); }
        }

        /// <summary>
        /// Học theo batch: dùng text gốc, chi tiết đã chốt và dự đoán để tìm lại dòng phù hợp.
        /// </summary>
        public async Task LearnAsync(
            Guid? customerId,
            string rawInput,
            IEnumerable<ChiTietHoaDonDto> finals,
            IEnumerable<QuickOrderDto> preds,
            IEnumerable<SanPhamDto> sanPhams)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return;

            var lines = NormalizeAndSplitLines(rawInput);
            var lineMap = lines.Select((txt, i) => (Line: i + 1, Text: txt))
                               .ToDictionary(x => x.Line, x => x.Text);

            // BienTheId -> SanPham
            var bt2sp = sanPhams.SelectMany(sp => sp.BienThe.Select(bt => (BienTheId: bt.Id, Sp: sp)))
                                .ToDictionary(x => x.BienTheId, x => x.Sp);

            // Gộp final theo sản phẩm
            var finalAgg = new Dictionary<Guid, (SanPhamDto sp, int qty)>();
            foreach (var ct in finals)
            {
                if (bt2sp.TryGetValue(ct.SanPhamIdBienThe, out var sp))
                {
                    if (!finalAgg.TryGetValue(sp.Id, out var cur))
                        finalAgg[sp.Id] = (sp, ct.SoLuong);
                    else
                        finalAgg[sp.Id] = (sp, cur.qty + ct.SoLuong);
                }
            }
            if (finalAgg.Count == 0) return;

            // Nếu preds có Line thì dùng thẳng
            var predsByLine = preds.Where(p => p != null && p.Line.HasValue)
                                   .GroupBy(p => p.Line!.Value)
                                   .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kv in finalAgg)
            {
                var spId = kv.Key;
                var sp = kv.Value.sp;
                var qty = Math.Max(1, kv.Value.qty);

                int? bestLine = null;

                // 1) Ưu tiên line do GPT trả về
                foreach (var g in predsByLine)
                {
                    if (g.Value.Any(v => v.Id == spId)) { bestLine = g.Key; break; }
                }

                // 2) Fallback token-overlap nếu không có line
                if (bestLine == null)
                {
                    string spName = Normalize(sp.Ten);
                    int bestScore = -1, bestLineTmp = -1;
                    foreach (var l in lineMap)
                    {
                        int sc = TokenOverlapScore(spName, l.Value);
                        if (sc > bestScore) { bestScore = sc; bestLineTmp = l.Key; }
                    }
                    if (bestScore > 0 && bestLineTmp > 0) bestLine = bestLineTmp;
                }

                if (bestLine != null)
                {
                    string rawLine = lineMap.TryGetValue(bestLine.Value, out var t) ? t : sp.Ten;
                    await UpsertEntryAsync(customerId, rawLine, sp.Id, sp.Ten, qty);
                }
            }

            await SaveAsync();
        }

        // =============== PUBLIC: SHORTLIST / PROMPT ===============

        /// <summary>Build shortlist tính điểm (theo khách + học chung) để chèn vào prompt.</summary>
        public List<(Guid id, string name, double score)> BuildShortlist(Guid? customerId, int topK = 12)
        {
            static double Decay(Entry x)
            {
                double lambda = Math.Log(2) / 30.0; // half-life ~30 ngày
                var days = (DateTime.Now - x.LastSeen).TotalDays;
                return x.Count * Math.Exp(-lambda * Math.Max(0, days));
            }

            List<(Guid id, string name, double score)> Agg(IEnumerable<Entry> src)
                => src.GroupBy(x => x.ProductId)
                      .Select(g => (g.Key,
                                    g.OrderByDescending(x => x.LastSeen).First().ProductName,
                                    g.Sum(Decay)))
                      .OrderByDescending(x => x.Item3)
                      .Take(topK)
                      .Select(x => (x.Key, x.Item2, x.Item3))
                      .ToList();

            var custTop = Agg(_model.Items.Where(x => x.CustomerId == customerId));
            var globalTop = Agg(_model.Items.Where(x => x.CustomerId == null));

            var merged = new Dictionary<Guid, (string name, double score)>();
            foreach (var it in custTop) merged[it.id] = (it.name, it.score + 1.0); // bias theo KH
            foreach (var it in globalTop) if (!merged.ContainsKey(it.id)) merged[it.id] = (it.name, it.score);

            return merged.OrderByDescending(kv => kv.Value.score)
                         .Select(kv => (kv.Key, kv.Value.name, kv.Value.score))
                         .ToList();
        }

        /// <summary>Xuất chuỗi SHORTLIST đúng format engine (Id&lt;TAB&gt;Tên).</summary>
        public string BuildShortlistForPrompt(
            Guid? customerId,
            IEnumerable<SanPhamDto> currentMenu,
            IEnumerable<(Guid id, string name)>? serverTopForCustomer = null,
            int topK = 12)
        {
            var byLearn = BuildShortlist(customerId, topK);

            if (serverTopForCustomer != null)
            {
                var set = new HashSet<Guid>(byLearn.Select(x => x.id));
                foreach (var (id, name) in serverTopForCustomer)
                    if (set.Add(id)) byLearn.Add((id, name, 0.75)); // bias nhẹ
            }

            var menuSet = new HashSet<Guid>(currentMenu.Select(m => m.Id));
            var lines = byLearn.Where(x => menuSet.Contains(x.id))
                               .Take(topK)
                               .Select(x => $"{x.id}\t{x.name}");

            return "SHORTLIST (Id<TAB>Tên)\n" + string.Join("\n", lines);
        }

        // ====================== INTERNALS ======================

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim().ToLowerInvariant();
            s = RemoveDiacritics(s);
            s = Regex.Replace(s, @"\s+", " ");
            return s;
        }

        private static string RemoveDiacritics(string text)
        {
            var norm = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in norm)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static IEnumerable<string> NormalizeAndSplitLines(string multiLine)
        {
            var list = new List<string>();
            using var reader = new StringReader(multiLine ?? "");
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var n = Normalize(line);
                if (!string.IsNullOrWhiteSpace(n)) list.Add(n);
            }
            return list;
        }

        private static int TokenOverlapScore(string a, string b)
        {
            var A = a.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var B = b.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (A.Length == 0 || B.Length == 0) return 0;
            var setB = new HashSet<string>(B);
            int c = 0; foreach (var t in A) if (setB.Contains(t)) c++;
            return c;
        }

        private static double Cosine(float[]? u, float[]? v)
        {
            if (u == null || v == null) return 0;
            int n = Math.Min(u.Length, v.Length);
            double dot = 0, uu = 0, vv = 0;
            for (int i = 0; i < n; i++) { dot += u[i] * v[i]; uu += u[i] * u[i]; vv += v[i] * v[i]; }
            if (uu == 0 || vv == 0) return 0;
            return dot / (Math.Sqrt(uu) * Math.Sqrt(vv));
        }

        private static double Similarity(string normLine, float[]? lineVec, Entry e)
        {
            double token = Math.Min(1.0, TokenOverlapScore(normLine, e.NormText) / 5.0);
            if (lineVec != null && e.Embedding != null)
                return 0.7 * Cosine(lineVec, e.Embedding) + 0.3 * token;
            return token;
        }

        private async Task UpsertEntryAsync(Guid? customerId, string rawLine, Guid productId, string productName, int qty = 1)
        {
            var norm = Normalize(rawLine);

            await _mu.WaitAsync();
            try
            {
                var e = _model.Items.FirstOrDefault(x =>
                    x.CustomerId == customerId &&
                    x.ProductId == productId &&
                    x.NormText == norm);

                if (e == null)
                {
                    e = new Entry
                    {
                        CustomerId = customerId,
                        LineText = rawLine,
                        NormText = norm,
                        ProductId = productId,
                        ProductName = productName,
                        Count = 0,
                        FirstSeen = DateTime.UtcNow
                    };
                    _model.Items.Add(e);
                }

                e.Count += Math.Max(1, qty);
                e.ProductName = productName;
                e.LastSeen = DateTime.UtcNow;

                if (e.Embedding == null)
                {
                    try { e.Embedding = await GetOrCreateEmbeddingAsync(norm); } catch { /* optional */ }
                }
            }
            finally { _mu.Release(); }
        }

        private async Task<float[]?> GetOrCreateEmbeddingAsync(string normText)
        {
            // Nếu có client embeddings, thay vào đây.
            await Task.CompletedTask;
            return null;
        }

        private async Task LoadAsync()
        {
            await _mu.WaitAsync();
            try
            {
                if (!File.Exists(_storePath)) return;
                var json = await File.ReadAllTextAsync(_storePath);
                var model = JsonSerializer.Deserialize<StoreModel>(json);
                if (model != null) _model = model;
            }
            finally { _mu.Release(); }
        }

        private async Task SaveAsync()
        {
            await _mu.WaitAsync();
            try
            {
                _model.LastSavedAt = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(_model, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_storePath, json);
            }
            finally { _mu.Release(); }
        }
    }
}
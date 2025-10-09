using System.IO;
using System.Text.Json;
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

        private Task _loadTask;
        private DateTime _lastSave = DateTime.MinValue;
        private bool _dirty = false;
        private static readonly TimeSpan SaveThrottle = TimeSpan.FromSeconds(3);

        private class Entry
        {
            public string LineText { get; set; } = "";
            public string NormText { get; set; } = "";
            public Guid ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public Guid? CustomerId { get; set; }
            public int Count { get; set; } = 0;
            public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
            public DateTime LastSeen { get; set; } = DateTime.UtcNow;
            public float[]? Embedding { get; set; }
        }

        private class StoreModel
        {
            public List<Entry> Items { get; set; } = new();
            public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;
            public int Version { get; set; } = 2;
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

            // ✅ chạy LoadAsync trong threadpool, tránh block UI
            _loadTask = Task.Run(() => LoadAsync());
        }

        // ================== PUBLIC: LEARN ==================
        public async Task LearnAsync(Guid? customerId, string lineText, Guid productId, string productName)
        {
            EnsureLoaded();

            var pre = OrderTextCleaner.PreClean(lineText);
            var norm = OrderTextCleaner.NormalizeNoDiacritics(pre);

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
                        LineText = StringHelper.NormalizeText(pre),
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

                if (e.Embedding == null)
                {
                    try { e.Embedding = await GetOrCreateEmbeddingAsync(norm); } catch { }
                }
            }
            finally { _mu.Release(); }

            await TrySaveThrottledAsync();
        }

        public async Task LearnAsync(
            Guid? customerId,
            string rawInput,
            IEnumerable<ChiTietHoaDonDto> finals,
            IEnumerable<QuickOrderDto> preds,
            IEnumerable<SanPhamDto> sanPhams)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(rawInput)) return;

            var lines = OrderTextCleaner.PreCleanThenNormalizeLines(rawInput).ToList();
            var lineMap = lines.Select((txt, i) => (Line: i + 1, Text: txt))
                               .ToDictionary(x => x.Line, x => x.Text);

            var bt2sp = sanPhams.SelectMany(sp => sp.BienThe.Select(bt => (bt.Id, Sp: sp)))
                                .ToDictionary(x => x.Id, x => x.Sp);

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

            var predsByLine = preds.Where(p => p != null && p.Line.HasValue)
                                   .GroupBy(p => p.Line!.Value)
                                   .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kv in finalAgg)
            {
                var sp = kv.Value.sp;
                int? bestLine = null;

                foreach (var g in predsByLine)
                    if (g.Value.Any(v => v.Id == sp.Id)) { bestLine = g.Key; break; }

                if (bestLine == null)
                {
                    string spName = OrderTextCleaner.NormalizeNoDiacritics(sp.Ten);
                    int bestScore = -1, bestLineTmp = -1;
                    foreach (var l in lineMap)
                    {
                        int sc = OrderTextCleaner.TokenOverlapScore(spName, l.Value);
                        if (sc > bestScore) { bestScore = sc; bestLineTmp = l.Key; }
                    }
                    if (bestScore > 0 && bestLineTmp > 0) bestLine = bestLineTmp;
                }

                if (bestLine != null)
                {
                    string rawLine = lineMap.TryGetValue(bestLine.Value, out var t) ? t : sp.Ten;
                    await UpsertEntryAsync(customerId, rawLine, sp.Id, sp.Ten, kv.Value.qty);
                }
            }

            await TrySaveThrottledAsync();
        }

        // =============== PUBLIC: SHORTLIST ===============
        public List<(Guid id, string name, double score)> BuildShortlist(Guid? customerId, int topK = 12)
        {
            EnsureLoaded();

            static double Decay(Entry x)
            {
                double lambda = Math.Log(2) / 30.0;
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
            foreach (var it in custTop) merged[it.id] = (it.name, it.score + 1.0);
            foreach (var it in globalTop) if (!merged.ContainsKey(it.id)) merged[it.id] = (it.name, it.score);

            return merged.OrderByDescending(kv => kv.Value.score)
                         .Select(kv => (kv.Key, kv.Value.name, kv.Value.score))
                         .ToList();
        }

        public string BuildShortlistForPrompt(
            Guid? customerId,
            IEnumerable<SanPhamDto> currentMenu,
            IEnumerable<(Guid id, string name)>? serverTopForCustomer = null,
            int topK = 12)
        {
            EnsureLoaded();

            var byLearn = BuildShortlist(customerId, topK);

            if (serverTopForCustomer != null)
            {
                var set = new HashSet<Guid>(byLearn.Select(x => x.id));
                foreach (var (id, name) in serverTopForCustomer)
                    if (set.Add(id)) byLearn.Add((id, name, 0.75));
            }

            var menuSet = new HashSet<Guid>(currentMenu.Select(m => m.Id));
            var lines = byLearn.Where(x => menuSet.Contains(x.id))
                               .Take(topK)
                               .Select(x => $"{x.id}\t{x.name}");

            return "SHORTLIST (Id<TAB>Tên)\n" + string.Join("\n", lines);
        }

        // ================= INTERNALS =================
        private async Task UpsertEntryAsync(Guid? customerId, string rawLine, Guid productId, string productName, int qty = 1)
        {
            var pre = OrderTextCleaner.PreClean(rawLine);
            var norm = OrderTextCleaner.NormalizeNoDiacritics(pre);

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
                        LineText = pre,
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
                    try { e.Embedding = await GetOrCreateEmbeddingAsync(norm); } catch { }
                }
            }
            finally { _mu.Release(); }
        }

        private async Task<float[]?> GetOrCreateEmbeddingAsync(string normText)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return null;
        }

        // ============= Load / Save (debounce) =============
        private void EnsureLoaded()
        {
            _loadTask?.GetAwaiter().GetResult();
        }

        private async Task LoadAsync()
        {
            await _mu.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!File.Exists(_storePath)) return;
                var json = await File.ReadAllTextAsync(_storePath).ConfigureAwait(false);
                var model = JsonSerializer.Deserialize<StoreModel>(json);
                if (model != null) _model = model;
            }
            finally
            {
                _mu.Release();
            }
        }

        private async Task TrySaveThrottledAsync()
        {
            await _mu.WaitAsync().ConfigureAwait(false);
            try
            {
                _dirty = true;
                if (DateTime.UtcNow - _lastSave >= SaveThrottle)
                {
                    await SaveToDiskLockedAsync().ConfigureAwait(false);
                }
            }
            finally { _mu.Release(); }
        }

        public async Task FlushAsync()
        {
            await _mu.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_dirty)
                {
                    await SaveToDiskLockedAsync().ConfigureAwait(false);
                }
            }
            finally { _mu.Release(); }
        }

        private async Task SaveToDiskLockedAsync()
        {
            _model.LastSavedAt = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(_model, new JsonSerializerOptions { WriteIndented = true });

            var tmp = _storePath + ".tmp";
            await File.WriteAllTextAsync(tmp, json).ConfigureAwait(false);

            if (File.Exists(_storePath))
            {
                var bak = _storePath + ".bak";
                try { File.Replace(tmp, _storePath, bak, ignoreMetadataErrors: true); }
                catch
                {
                    File.Copy(tmp, _storePath, overwrite: true);
                }
                finally
                {
                    try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                }
            }
            else
            {
#if NET6_0_OR_GREATER
                File.Move(tmp, _storePath, overwrite: true);
#else
                File.Copy(tmp, _storePath, overwrite: true);
                try { File.Delete(tmp); } catch { }
#endif
            }

            _lastSave = DateTime.UtcNow;
            _dirty = false;
        }
    }
}
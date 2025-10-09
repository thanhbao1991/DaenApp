using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Shared.Helpers
{
    /// <summary>
    /// Chuẩn hoá text đơn hàng (Messenger, GPT, v.v.)
    /// Dùng dữ liệu từ DB TuDienTraCuu (Ten → TenPhienDich)
    /// - DB lưu Ten KHÔNG DẤU (đã chuẩn hoá trước khi lưu)
    /// - Nếu chỉ 1 chữ: tra theo key KHÔNG DẤU nhưng giữ dấu ở value (TenPhienDich)
    /// - Nếu nhiều chữ: tra theo CỤM KHÔNG DẤU
    /// - Thay thế đa bước (multi-pass): ví dụ "nc dua" → (pass1) "nước dua" → (pass2) "nước dừa"
    /// </summary>
    public static class OrderTextCleaner
    {
        // ===== Cache TuDienTraCuu (tự refresh mỗi 10 phút) =====
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
        private static DateTime _lastLoaded = DateTime.MinValue;

        // Hai từ điển cache:
        //  - _dictExact: key KHÔNG DẤU cho 1 từ → value có dấu (TenPhienDich)
        //  - _dictNormalized: key KHÔNG DẤU cho cụm >= 2 từ → value có dấu (TenPhienDich)
        private static Dictionary<string, string> _dictExact = new(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _dictNormalized = new(StringComparer.OrdinalIgnoreCase);

        private static void EnsureCacheLoaded()
        {
            try
            {
                if ((_dictExact.Count == 0 && _dictNormalized.Count == 0) || DateTime.UtcNow - _lastLoaded > CacheDuration)
                {
                    var list = AppProviders.TuDienTraCuus?.Items ?? new ObservableCollection<TuDienTraCuuDto>();
                    var active = list.Where(x => x.DangSuDung && !x.IsDeleted).ToList();

                    // 1 chữ → lưu key KHÔNG DẤU
                    _dictExact = active
                        .Where(x => (x.Ten ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 1)
                        .ToDictionary(
                            x => StringHelper.NormalizeText((x.Ten ?? "").Trim()),
                            x => (x.TenPhienDich ?? "").Trim(),
                            StringComparer.OrdinalIgnoreCase
                        );

                    // nhiều chữ → lưu key KHÔNG DẤU (cả cụm)
                    _dictNormalized = active
                        .Where(x => (x.Ten ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 1)
                        .ToDictionary(
                            x => StringHelper.NormalizeText((x.Ten ?? "").Trim()),
                            x => (x.TenPhienDich ?? "").Trim(),
                            StringComparer.OrdinalIgnoreCase
                        );

                    _lastLoaded = DateTime.UtcNow;
                }
            }
            catch
            {
                // nếu AppProviders chưa sẵn sàng thì bỏ qua, sẽ load lại lần sau
            }
        }

        // 🟟 Thay từ rút gọn bằng từ chuẩn với cơ chế ĐA BƯỚC
        // - Ưu tiên thay cụm (từ dài đến ngắn), key tra KHÔNG DẤU
        // - Nếu không có cụm: thay từng từ đơn (key KHÔNG DẤU)
        // - Lặp lại tối đa 3 lần đến khi không thay nữa (để hỗ trợ chuỗi thay thế bắc cầu: "nc dua" → "nước dừa")
        private static string ApplyWordReplacements(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            EnsureCacheLoaded();

            string prev;
            string current = input.Trim();
            int maxPass = 3;

            do
            {
                prev = current;

                // token hoá dạng gốc & dạng KHÔNG DẤU để dò từ điển
                var normTokens = StringHelper.NormalizeText(current)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                var origTokens = current.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

                if (normTokens.Count == 0) break;

                bool replacedAny = false;

                // 1) Ưu tiên thay theo CỤM (từ dài → ngắn) với key KHÔNG DẤU
                for (int len = Math.Min(4, normTokens.Count); len >= 2; len--)
                {
                    for (int i = 0; i <= normTokens.Count - len; i++)
                    {
                        var key = string.Join(' ', normTokens.Skip(i).Take(len)); // KHÔNG DẤU
                        if (_dictNormalized.TryGetValue(key, out var replacement))
                        {
                            // Thay thế đúng vị trí trên chuỗi gốc
                            origTokens.RemoveRange(i, len);
                            origTokens.Insert(i, replacement);
                            current = string.Join(' ', origTokens);
                            replacedAny = true;
                            goto NEXT_PASS; // tính lại token ở vòng sau
                        }
                    }
                }

                // 2) Không có cụm → thử thay TỪ ĐƠN (key KHÔNG DẤU)
                for (int i = 0; i < normTokens.Count; i++)
                {
                    var key = normTokens[i]; // KHÔNG DẤU
                    if (_dictExact.TryGetValue(key, out var repl))
                    {
                        origTokens[i] = repl;
                        replacedAny = true;
                    }
                }
                current = string.Join(' ', origTokens);

            NEXT_PASS:
                if (!replacedAny) break;

            } while (!string.Equals(current, prev, StringComparison.OrdinalIgnoreCase) && --maxPass > 0);

            return current;
        }

        // =========================
        // ⚙️ CHUẨN HOÁ CƠ BẢN
        // =========================
        public static string PreClean(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.Trim();
            s = Regex.Replace(s, @"[^\p{L}\p{N}\s\.,%-/]", " "); // giữ .,%-/
            s = Regex.Replace(s, @"\s+", " ");
            return s.Trim();
        }

        public static string NormalizeNoDiacritics(string input) => input.Trim();

        private static readonly Regex _rxInt = new(@"\b\d+\b", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex _rxKAmount = new(@"\b(\d{1,3})\s*[kK]\b", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        // “25k” → “25000”
        public static string ExpandKNotationNumbers(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            return _rxKAmount.Replace(input, m =>
            {
                if (!int.TryParse(m.Groups[1].Value, out var n)) return m.Value;
                return (n * 1000).ToString();
            });
        }

        // “25” → “25000” (né giờ)
        public static string InflateNumbersToThousands(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            return _rxInt.Replace(input, m =>
            {
                var numStr = m.Value;
                if (!int.TryParse(numStr, out var n)) return numStr;

                int end = m.Index + m.Length;
                if (end < input.Length)
                {
                    char next = input[end];
                    if ((next == ':' || next == 'h' || next == 'H') &&
                        end + 1 < input.Length && char.IsDigit(input[end + 1]))
                        return numStr;
                }

                if (n < 15 || n >= 1000 || numStr.EndsWith("000"))
                    return numStr;

                return numStr + "000";
            });
        }

        // =========================
        // 🟟 CHUẨN HOÁ DÒNG CHAT
        // =========================
        public static IEnumerable<string> PreCleanThenNormalizeLines(string multiLine, string? customerNameHint = null)
        {
            var list = new List<string>();
            var ignoreNameNorm = (customerNameHint ?? "").Trim();

            using var reader = new StringReader(multiLine ?? "");
            string? line;
            bool skipNvBlock = false;

            while ((line = reader.ReadLine()) != null)
            {
                var raw = line.Trim();
                if (string.IsNullOrEmpty(raw)) continue;

                if (IsEnterLine(raw)) { skipNvBlock = false; continue; }

                var lower = raw.ToLowerInvariant();
                if (lower.StartsWith("bạn đã gửi") || lower.StartsWith("ban da gui") ||
                    lower.StartsWith("đã gửi") || lower.StartsWith("da gui"))
                {
                    skipNvBlock = true;
                    continue;
                }
                if (skipNvBlock) continue;

                if (IsStandaloneClockTime(raw)) continue;
                if (!string.IsNullOrEmpty(ignoreNameNorm) && ignoreNameNorm.Contains(raw)) continue;

                var pre = PreClean(raw);
                var n = NormalizeNoDiacritics(pre);
                n = ApplyWordReplacements(n);
                if (!string.IsNullOrWhiteSpace(n))
                {
                    // nếu dòng chỉ là giờ "13 20" thì bỏ qua
                    if (Regex.IsMatch(n, @"^(?:[01]?\d|2[0-3])\s+\d{2}(?:\s*(?:am|pm))?$")) continue;

                    n = ExpandKNotationNumbers(n);
                    n = InflateNumbersToThousands(n);
                    list.Add(n);
                }
            }

            // Bỏ dòng 1 từ lặp nhiều lần (anti-noise)
            var oneWordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in list)
            {
                var toks = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (toks.Length == 1)
                {
                    var key = toks[0];
                    oneWordCounts[key] = oneWordCounts.TryGetValue(key, out var c) ? c + 1 : 1;
                }
            }

            list = list.Where(s =>
            {
                var toks = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (toks.Length != 1) return true;
                return oneWordCounts[toks[0]] <= 1;
            }).ToList();

            return list;
        }

        private static bool IsEnterLine(string s)
            => s.Trim().Equals("enter", StringComparison.OrdinalIgnoreCase);

        private static bool IsStandaloneClockTime(string s)
        {
            return Regex.IsMatch(s.Trim(),
                @"^(?:[01]?\d|2[0-3])\s*(?::|h)\s*\d{2}\s*(?:am|pm)?$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        public static int TokenOverlapScore(string a, string b)
        {
            var toksA = NormalizeNoDiacritics(a).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var toksB = NormalizeNoDiacritics(b).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (toksA.Length == 0 || toksB.Length == 0) return 0;
            return toksA.Intersect(toksB).Count();
        }

        // ======================================================================
        // 🟟 PHÂN VAI HỘI THOẠI MESSENGER → KH/NV
        // ======================================================================
        public static string BuildChatContextFromMessenger(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            var lines = raw.Replace("\r", "")
                           .Replace("Enter", "\n", StringComparison.OrdinalIgnoreCase)
                           .Split('\n')
                           .Select(s => s.Trim())
                           .Where(s => !string.IsNullOrWhiteSpace(s))
                           .ToList();

            var result = new List<string>();
            string? pendingKh = null;
            bool nvMode = false;

            foreach (var line in lines)
            {
                var lower = line.ToLowerInvariant();

                if (Regex.IsMatch(lower, @"^\d{1,2}:\d{2}(\s*(am|pm))?$")) continue;
                if (IsOnlyName(line)) continue;

                if (lower.StartsWith("bạn đã gửi") || lower.StartsWith("ban da gui"))
                {
                    if (!string.IsNullOrWhiteSpace(pendingKh))
                    {
                        result.Add("KH: " + pendingKh.Trim());
                        pendingKh = null;
                    }
                    nvMode = true;
                    continue;
                }

                if (line.StartsWith("."))
                {
                    if (!string.IsNullOrWhiteSpace(pendingKh))
                    {
                        result.Add("KH: " + pendingKh.Trim());
                        pendingKh = null;
                    }
                    var msg = line.TrimStart('.');
                    if (!string.IsNullOrWhiteSpace(msg))
                        result.Add("NV: " + msg.Trim());
                    nvMode = false;
                    continue;
                }

                if (nvMode)
                {
                    result.Add("NV: " + line);
                    nvMode = false;
                    continue;
                }

                if (pendingKh == null)
                    pendingKh = line;
                else
                {
                    if (ShouldFlushKhOnNewLine(pendingKh, line))
                    {
                        result.Add("KH: " + pendingKh.Trim());
                        pendingKh = line;
                    }
                    else
                    {
                        pendingKh += " " + line;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(pendingKh))
                result.Add("KH: " + pendingKh.Trim());

            if (result.Count > 8)
                result = result.TakeLast(8).ToList();

            return string.Join("\n", result);
        }

        private static bool IsOnlyName(string s)
        {
            return Regex.IsMatch(s, @"^[A-ZÀ-ỹ][\p{L}\p{M}\s]{0,30}$", RegexOptions.CultureInvariant);
        }

        private static bool ShouldFlushKhOnNewLine(string prev, string line)
        {
            if (Regex.IsMatch(prev, @"[\.!\?:]$")) return true;
            if (line.Length <= 22) return true;

            var l = NormalizeNoDiacritics(line);
            if (Regex.IsMatch(l, @"^(it|bot|them|giam|khong|cho|lay|ship|giao|in|ghi|doi(\s+size)?|them\s+da|bot\s+da|it\s+da|it\s+ngot)\b"))
                return true;

            return false;
        }

        // ======================================================================
        // ✂️ Trích riêng dòng KH (đã chuẩn hoá)
        // ======================================================================
        public static List<string> ExtractKhNormalizedLines(string chatOrRaw, int keepLast = 8)
        {
            var chat = (chatOrRaw?.Contains("KH:") == true || chatOrRaw?.Contains("NV:") == true)
                ? chatOrRaw
                : BuildChatContextFromMessenger(chatOrRaw ?? "");

            var lines = chat.Replace("\r", "").Split('\n')
                            .Select(s => s.Trim())
                            .Where(s => s.StartsWith("KH:"))
                            .ToList();

            if (lines.Count > keepLast)
                lines = lines.TakeLast(keepLast).ToList();

            var result = new List<string>();
            foreach (var ln in lines)
            {
                var payload = ln.Substring(3).Trim();
                var pre = PreClean(payload);
                var norm = NormalizeNoDiacritics(pre);
                norm = ApplyWordReplacements(norm);
                if (!string.IsNullOrWhiteSpace(norm))
                {
                    norm = ExpandKNotationNumbers(norm);
                    norm = InflateNumbersToThousands(norm);
                    result.Add(norm);
                }
            }
            return result;
        }
    }
}
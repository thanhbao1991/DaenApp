using System.Text;

namespace TraSuaApp.WpfClient.GptOrders
{
    public static class AutoLearnManager
    {
        private static readonly HashSet<string> _fillerSeedNoDiacritics = new(StringComparer.OrdinalIgnoreCase)
        {
            "chi","anh","em","ban","oi","nha","nhe","ha","hen","di","ne","nha a","nha e","nhe a","nhe e","nhe nha","nhe nhe",
            "hy","hi","hi","hí","hy","hai","haiz"
        };

        public static string StripDiacriticsVN(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var norm = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(s.Length);
            foreach (var ch in norm)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            var r = sb.ToString().Normalize(NormalizationForm.FormC);
            return r.Replace('đ', 'd').Replace('Đ', 'D');
        }

        /// <summary>
        /// Tự học từ các dòng bị MISS: nhận diện token "filler" & biến thể không dấu.
        /// </summary>
        public static bool LearnFromFailure(IReadOnlyList<string> rawLines, IReadOnlyList<int> missedLineIndexes, QuickOrderMemory mem)
        {
            bool changed = false;
            foreach (var idx in missedLineIndexes)
            {
                if (idx < 1 || idx > rawLines.Count) continue;
                var line = rawLines[idx - 1];

                foreach (var token in line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var t = token.Trim().ToLowerInvariant();
                    if (t.Length > 16) continue; // tránh noise dài
                    var tNo = StripDiacriticsVN(t);

                    if (_fillerSeedNoDiacritics.Contains(tNo))
                    {
                        if (!mem.Fillers.Contains(t, StringComparer.OrdinalIgnoreCase))
                        {
                            mem.Fillers.Add(t);
                            changed = true;
                        }
                        if (!mem.Fillers.Contains(tNo, StringComparer.OrdinalIgnoreCase))
                        {
                            mem.Fillers.Add(tNo);
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                mem.UpdatedAt = DateTime.UtcNow;
                QuickOrderMemory.Save(mem);
            }
            return changed;
        }

        /// <summary>
        /// Học nhanh từ ca thành công: nếu user thường gõ tắt nào đó xuất hiện,
        /// thêm vào ShortcutBias để prompt ưu tiên hiểu.
        /// </summary>
        public static bool LearnFromSuccess(IReadOnlyList<string> rawLines, QuickOrderMemory mem)
        {
            bool changed = false;

            foreach (var line in rawLines)
            {
                var ln = line.Trim().ToLowerInvariant();
                // ví dụ heuristic: nếu chứa "c phe" hay "cafe" → thêm "c phe", "cf", "cafe"
                if (ln.Contains("c phe") || ln.Contains("ca phe") || ln.Contains("cafe"))
                {
                    changed |= TryAdd(mem.ShortcutBias, "cf");
                    changed |= TryAdd(mem.ShortcutBias, "cafe");
                    changed |= TryAdd(mem.ShortcutBias, "phe");
                }
                if (ln.Contains("tcdd") || ln.Contains("tran chau duong den"))
                {
                    changed |= TryAdd(mem.ShortcutBias, "tcdd");
                }
            }

            if (changed)
            {
                mem.UpdatedAt = DateTime.UtcNow;
                QuickOrderMemory.Save(mem);
            }
            return changed;
        }

        private static bool TryAdd(List<string> list, string v)
        {
            if (!list.Contains(v, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(v);
                return true;
            }
            return false;
        }
    }
}
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TraSuaApp.WpfClient.AiOrdering
{
    /// <summary>
    /// Tiền xử lý text order: mở rộng viết tắt, loại từ đệm/lịch sự,
    /// giữ note quan trọng (đá riêng, ít đường...), và chuẩn hoá không dấu.
    /// Dùng chung cho Engine (parse) và Learning Store (học).
    /// </summary>
    public static class OrderTextCleaner
    {
        // Viết tắt phổ biến → tên đầy đủ
        private static readonly Dictionary<string, string> SlangMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["cf"] = "cà phê",
            ["cfe"] = "cà phê",
            ["caphe"] = "cà phê",
            ["ts"] = "trà sữa",
            ["trasua"] = "trà sữa",
            ["ol"] = "olong",
            ["tc"] = "trân châu",
        };

        // Các note cần giữ lại và chuẩn hoá về dạng thống nhất
        private static readonly (Regex rx, string keep)[] KeepNotePatterns = new (Regex, string)[]
        {
            (new Regex(@"\b(đá|da)\s*(riêng|rieng)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "đá riêng"),
            (new Regex(@"\b(ít|it)\s*(đá|da)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "ít đá"),
            (new Regex(@"\b(kh(ô|o)ng)\s*(đá|da)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "không đá"),
            (new Regex(@"\b(ít|it)\s*(đường|duong)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "ít đường"),
            (new Regex(@"\b(kh(ô|o)ng)\s*(đường|duong)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "không đường"),
        };

        /// <summary>
        /// Tiền xử lý 1 dòng: mở rộng viết tắt, rút trích note (đá riêng...),
        /// bỏ từ đệm/lịch sự, làm sạch ký tự. Trả về chuỗi có thể vẫn còn dấu (để hiển thị),
        /// note được đưa về cuối câu.
        /// </summary>
        public static string PreClean(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";

            var work = s;

            // 1) Mở rộng viết tắt theo token
            work = Regex.Replace(work, @"\b[\p{L}\p{M}]+\b", m =>
            {
                var token = m.Value;
                if (SlangMap.TryGetValue(token.ToLowerInvariant(), out var rep)) return rep;
                return token;
            });

            // 2) Rút trích note cần giữ
            var notes = new List<string>();
            foreach (var (rx, keep) in KeepNotePatterns)
            {
                if (rx.IsMatch(work)) notes.Add(keep);
                work = rx.Replace(work, " ");
            }

            // 3) Loại bỏ từ đệm/lịch sự/rác
            work = Regex.Replace(work,
                @"\b(anh|chị|chi|em|a|c|nha|nhé|nhe|giúp|giup|giùm|gium|dùm|dum|với|voi|ạ|ak|pls|plz|thanks?|tks|cho|minh|mình)\b\.?",
                "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            // 4) Làm sạch ký tự & khoảng trắng
            work = work.Replace('\u00A0', ' ')
                       .Replace("\u200B", "").Replace("\u200C", "").Replace("\u200D", "").Replace("\uFEFF", "");
            work = Regex.Replace(work, @"[^\p{L}\p{M}\p{N}\s]+", " ");
            work = Regex.Replace(work, @"\s+", " ").Trim();

            // 5) Gắn note về cuối chuỗi (để GPT/learning thấy)
            if (notes.Count > 0)
            {
                var note = string.Join(". ", notes.Distinct());
                work = string.IsNullOrWhiteSpace(work) ? note : $"{work}. {note}";
            }

            return work;
        }

        /// <summary>Chuẩn hoá: trim → lowercase → bỏ dấu → gộp space.</summary>
        public static string NormalizeNoDiacritics(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim().ToLowerInvariant();
            s = RemoveDiacritics(s);
            s = Regex.Replace(s, @"\s+", " ");
            return s;
        }

        /// <summary>Tách multi-line → PreClean từng dòng → Normalize → lọc rỗng.</summary>
        public static IEnumerable<string> PreCleanThenNormalizeLines(string multiLine)
        {
            var list = new List<string>();
            using var reader = new StringReader(multiLine ?? "");
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var pre = PreClean(line);
                var n = NormalizeNoDiacritics(pre);
                if (!string.IsNullOrWhiteSpace(n)) list.Add(n);
            }
            return list;
        }

        /// <summary>Điểm giao nhau token (đơn giản).</summary>
        public static int TokenOverlapScore(string a, string b)
        {
            var A = (a ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var B = (b ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (A.Length == 0 || B.Length == 0) return 0;
            var setB = new HashSet<string>(B);
            int c = 0; foreach (var t in A) if (setB.Contains(t)) c++;
            return c;
        }

        private static string RemoveDiacritics(string text)
        {
            var norm = (text ?? "").Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in norm)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
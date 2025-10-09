using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TraSuaApp.Shared.Helpers;

public static class StringHelper
{
    // ⚡️ Regex cache sẵn để tránh compile lại
    private static readonly Regex MultiSpaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex SqlCommentPattern = new(@"(--.*?$)|(/\*.*?\*/)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline);
    private static readonly Regex DangerousCharsPattern = new(@"['""`;`]|[\x00-\x1F\x7F]+", RegexOptions.Compiled);

    private static readonly HashSet<string> MinorWords = new(StringComparer.OrdinalIgnoreCase)
    { "và", "của", "the", "in", "on", "at", "by", "for", "of", "to", "a", "an" };

    // =============================
    // 🟟 1. Extension NormalizeText
    // =============================
    public static string MyNormalizeText(this string? input, bool ignoreAccentOnly = false)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        // Loại bỏ ký tự ẩn hoặc không mong muốn
        input = input
            .Replace("\u200B", "") // zero-width space
            .Replace("\uFEFF", "") // BOM
            .Replace("“", "\"").Replace("”", "\"")
            .Replace("‘", "'").Replace("’", "'");

        var sb = new StringBuilder(input.Length);
        bool lastSpace = false;

        foreach (var c in input.Normalize(NormalizationForm.FormD))
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastSpace) sb.Append(' ');
                lastSpace = true;
                continue;
            }

            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark)
            {
                char ch = c switch
                {
                    'Đ' => 'd',
                    'đ' => 'd',
                    _ => c
                };
                sb.Append(ignoreAccentOnly ? ch : char.ToLowerInvariant(ch));
                lastSpace = false;
            }
        }

        var result = sb.ToString().Trim();
        if (!ignoreAccentOnly)
            result = MultiSpaceRegex.Replace(result, " ");

        // ✅ Bỏ các ký tự ngoặc, dấu câu, ký tự đặc biệt
        result = new string(result.Where(c =>
            char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)
        ).ToArray());

        return result;
    }
    //public static string NormalizeText(this string? input, bool ignoreAccentOnly = false)
    //{
    //    if (string.IsNullOrWhiteSpace(input)) return "";

    //    // Loại bỏ ký tự ẩn hoặc không mong muốn
    //    input = input
    //        .Replace("\u200B", "") // zero-width space
    //        .Replace("\uFEFF", "") // BOM
    //        .Replace("“", "\"").Replace("”", "\"")
    //        .Replace("‘", "'").Replace("’", "'");

    //    var sb = new StringBuilder(input.Length);
    //    bool lastSpace = false;

    //    foreach (var c in input.Normalize(NormalizationForm.FormD))
    //    {
    //        if (char.IsWhiteSpace(c))
    //        {
    //            if (!lastSpace) sb.Append(' ');
    //            lastSpace = true;
    //            continue;
    //        }

    //        var cat = CharUnicodeInfo.GetUnicodeCategory(c);
    //        if (cat != UnicodeCategory.NonSpacingMark)
    //        {
    //            char ch = c switch
    //            {
    //                'Đ' => 'd',
    //                'đ' => 'd',
    //                _ => c
    //            };
    //            sb.Append(ignoreAccentOnly ? ch : char.ToLowerInvariant(ch));
    //            lastSpace = false;
    //        }
    //    }

    //    var result = sb.ToString().Trim();
    //    if (!ignoreAccentOnly)
    //        result = MultiSpaceRegex.Replace(result, " ");

    //    return result;
    //}

    // ✅ Alias gọi tĩnh (nếu cần)
    public static string NormalizeText(string? s) => s.MyNormalizeText();

    // =============================
    // 🟟 2. So sánh nhanh
    // =============================
    public static bool EqualsNormalized(this string? a, string? b)
        => a.MyNormalizeText() == b.MyNormalizeText();

    // =============================
    // 🟟 3. Lấy tên rút gọn
    // =============================
    public static string GetShortName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string normalized = input.MyNormalizeText();
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var shortName = new StringBuilder();
        foreach (var word in words)
        {
            if (string.IsNullOrWhiteSpace(word))
                continue;

            // Nếu từ bắt đầu bằng số hoặc ký tự có số → lấy toàn bộ
            if (char.IsDigit(word[0]) || (word.Length > 1 && char.IsDigit(word[1])))
                shortName.Append(word);
            else
                shortName.Append(word[0]);
        }

        return shortName.ToString().ToLower();
    }

    // =============================
    // 🟟 4. Viết hoa mỗi từ (smart)
    // =============================
    public static string? CapitalizeEachWord(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var words = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            var w = words[i].ToLowerInvariant();
            if (i == 0 || !MinorWords.Contains(w))
                words[i] = char.ToUpper(w[0]) + w[1..];
            else
                words[i] = w;
        }

        return string.Join(' ', words);
    }

    // =============================
    // 🟟 5. SQL Sanitize
    // =============================
    private static string SoftSanitizeSql(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? "";

        input = SqlCommentPattern.Replace(input, ""); // Xoá comment
        input = DangerousCharsPattern.Replace(input, ""); // Xoá ký tự nguy hiểm
        input = input.Replace("'", "''").Replace("\"", "\"\"");
        input = MultiSpaceRegex.Replace(input, " ").Trim();

        return input;
    }

    private static string StrictSanitizeSql(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? "";

        input = SoftSanitizeSql(input);

        var sqlKeywords = new[] {
            "SELECT","INSERT","UPDATE","DELETE","DROP","TRUNCATE","ALTER",
            "EXEC","EXECUTE","UNION","MERGE","CREATE","REPLACE","GRANT","REVOKE",
            "WHERE","OR","AND"
        };

        foreach (var kw in sqlKeywords)
        {
            input = Regex.Replace(input, $@"\b{Regex.Escape(kw)}\b", "",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        return MultiSpaceRegex.Replace(input, " ").Trim();
    }

    // =============================
    // 🟟 6. Chuẩn hoá tất cả string trong object
    // =============================
    public static void NormalizeAllStrings<T>(T obj, bool sanitizeSql = true, bool strictSqlSanitize = false)
    {
        if (obj == null) return;

        var stringProps = obj.GetType()
            .GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

        foreach (var prop in stringProps)
        {
            var value = prop.GetValue(obj) as string;
            if (string.IsNullOrEmpty(value)) continue;

            if (sanitizeSql)
                value = strictSqlSanitize ? StrictSanitizeSql(value) : SoftSanitizeSql(value);

            value = CapitalizeEachWord(value);
            prop.SetValue(obj, value);
        }
    }

#if DEBUG
    // =============================
    // 🟟 7. Test nhanh trong Debug
    // =============================
    public static void TestNormalize(params string[] inputs)
    {
        foreach (var s in inputs)
        {
            Console.WriteLine($"{s} → {s.MyNormalizeText()}");
        }
    }
#endif
}
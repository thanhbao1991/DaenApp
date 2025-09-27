using System.Text.RegularExpressions;

namespace TraSuaApp.Shared.Helpers;

public static class StringHelper
{
    public static string? CapitalizeEachWord(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var words = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            var w = words[i];
            words[i] = char.ToUpper(w[0]) + w.Substring(1).ToLower();
        }
        return string.Join(" ", words);
    }

    // ✅ Sanitize nhẹ nhàng (chỉ loại bỏ ký tự nguy hiểm phổ biến)
    private static string SoftSanitizeSql(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? "";

        // Xoá comment SQL
        input = Regex.Replace(input, @"--.*?$", "", RegexOptions.Multiline);
        input = Regex.Replace(input, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Escape thay vì xóa hẳn để giữ nguyên nghĩa dữ liệu
        input = input.Replace("'", "''");   // escape nháy đơn
        input = input.Replace("\"", "\"\""); // escape nháy kép

        // Xoá các ký tự nguy hiểm khác
        input = input.Replace(";", "");
        input = input.Replace("`", "");
        input = input.Replace("--", "");

        // Xoá control chars
        input = Regex.Replace(input, @"[\x00-\x1F\x7F]+", "");

        // Chuẩn hóa khoảng trắng
        input = Regex.Replace(input, @"\s{2,}", " ").Trim();

        return input;
    }

    // ✅ Sanitize nghiêm ngặt (ngoài ký tự, xoá từ khoá SQL)
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
                RegexOptions.IgnoreCase);
        }

        input = Regex.Replace(input, @"\s{2,}", " ").Trim();
        return input;
    }

    /// <summary>
    /// Chuẩn hoá tất cả string property trong object:
    /// - Sanitize SQL injection
    /// - Capitalize chữ cái đầu mỗi từ
    /// </summary>
    public static void NormalizeAllStrings<T>(T obj, bool sanitizeSql = true, bool strictSqlSanitize = false)
    {
        if (obj == null) return;

        var stringProps = obj.GetType()
            .GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

        foreach (var prop in stringProps)
        {
            var value = prop.GetValue(obj) as string;
            if (!string.IsNullOrEmpty(value))
            {
                if (sanitizeSql)
                {
                    value = strictSqlSanitize ? StrictSanitizeSql(value) : SoftSanitizeSql(value);
                }

                value = CapitalizeEachWord(value);
                prop.SetValue(obj, value);
            }
        }
    }
}
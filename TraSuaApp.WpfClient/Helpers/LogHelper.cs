using System.Text;
using System.Text.Json;
using TraSuaApp.Shared.Dtos;

public static class LogHelper
{
    public static string RutGonLog(LogDto log)
    {
        string doiTuong = GetTenDoiTuong(log.Path);
        string? tenChinh = LayTenChinh(log.ResponseBodyShort ?? log.RequestBodyShort);
        string hanhDong = log.Method?.ToUpper() switch
        {
            "POST" => "thêm",
            "PUT" => "cập nhật",
            "DELETE" => "xoá",
            _ => "thao tác"
        };

        return $"{log.UserName} đã {hanhDong} {doiTuong.ToLower()}{(string.IsNullOrEmpty(tenChinh) ? "" : $": {tenChinh}")}";
    }

    public static string ChiTietLog(LogDto log)
    {
        try
        {
            var sb = new StringBuilder();

            JsonElement? before = TryGetJsonProperty(log.ResponseBodyShort, "beforeData");
            JsonElement? after = TryGetJsonProperty(log.ResponseBodyShort, "afterData");
            JsonElement? data = TryGetJsonProperty(log.ResponseBodyShort, "data");

            string method = log.Method?.ToUpper() ?? "";

            if (method == "POST")
            {
                sb.AppendLine("➕ Đã thêm:");
                sb.AppendLine(FormatObject(after ?? data));
            }
            else if (method == "PUT")
            {
                sb.AppendLine("🟟 Trước khi cập nhật:");
                sb.AppendLine(FormatObject(before));
                sb.AppendLine();
                sb.AppendLine("✅ Sau khi cập nhật:");
                sb.AppendLine(FormatObject(after ?? data));
            }
            else if (method == "DELETE")
            {
                sb.AppendLine("🟟️ Đã xoá:");
                sb.AppendLine(FormatObject(data ?? before));
            }
            else
            {
                sb.AppendLine("🟟 Thao tác:");
                sb.AppendLine(FormatObject(data ?? after ?? before));
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Không thể phân tích chi tiết log. Lỗi: {ex.Message}";
        }
    }

    private static JsonElement? TryGetJsonProperty(string? json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(propertyName, out var prop))
                return prop;
        }
        catch { }
        return null;
    }

    private static string FormatObject(JsonElement? element)
    {
        if (element == null || element.Value.ValueKind == JsonValueKind.Null) return "(không có dữ liệu)";
        return JsonSerializer.Serialize(element.Value, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string GetTenDoiTuong(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "(không rõ đối tượng)";
        var match = System.Text.RegularExpressions.Regex.Match(path.ToLower(), @"\/?api\/?([a-zA-Z0-9]+)");
        if (!match.Success) return "(không rõ đối tượng)";
        var key = match.Groups[1].Value.ToLower();
        return TraSuaApp.Shared.Enums.TuDien._tableFriendlyNames.TryGetValue(key, out var name)
            ? name
            : key;
    }

    private static string? LayTenChinh(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Nếu có trường `data` hoặc `afterData`, lấy nó
            if (root.TryGetProperty("data", out var data)) root = data;
            else if (root.TryGetProperty("afterData", out var after)) root = after;

            string[] fields = { "ten", "Ten", "title", "Title", "name", "Name" };
            foreach (var field in fields)
            {
                if (root.TryGetProperty(field, out var value))
                    return value.GetString();
            }
        }
        catch { }
        return null;
    }
}
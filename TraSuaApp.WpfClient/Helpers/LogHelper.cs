using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;

namespace TraSuaApp.Shared.Helpers;

public static class LogHelper
{
    public static string ChiTietLog(LogDto log)
    {
        var sb = new StringBuilder();

        try
        {
            var json = JsonDocument.Parse(log.ResponseBodyShort ?? "");
            var root = json.RootElement;

            JsonElement beforeData = default;
            JsonElement afterData = default;

            bool hasBefore = root.TryGetProperty("beforeData", out beforeData);
            bool hasAfter = root.TryGetProperty("afterData", out afterData);

            if (!hasBefore && !hasAfter)
            {
                sb.AppendLine("⚠️ (không có hoặc không xác định)");
                return sb.ToString();
            }

            // XÓA
            if (hasBefore && (!hasAfter || afterData.ValueKind == JsonValueKind.Null))
            {
                sb.AppendLine("✘ Đã xoá bản ghi:");
                foreach (var prop in beforeData.EnumerateObject())
                {
                    sb.AppendLine($"  ▸ {prop.Name}: {FormatValue(prop.Value)}");
                }
                return sb.ToString();
            }

            // THÊM (đã sửa điều kiện)
            if ((!hasBefore || beforeData.ValueKind == JsonValueKind.Null)
                && hasAfter && afterData.ValueKind != JsonValueKind.Null)
            {
                sb.AppendLine("+ Đã thêm bản ghi:");
                foreach (var prop in afterData.EnumerateObject())
                {
                    sb.AppendLine($"  ▸ {prop.Name}: {FormatValue(prop.Value)}");
                }
                return sb.ToString();
            }

            // SỬA
            if (hasBefore && hasAfter && beforeData.ValueKind != JsonValueKind.Null && afterData.ValueKind != JsonValueKind.Null)
            {
                sb.AppendLine("✓ Cập nhật:");
                bool coThayDoi = false;

                foreach (var prop in afterData.EnumerateObject())
                {
                    if (beforeData.TryGetProperty(prop.Name, out var beforeProp))
                    {
                        if (!JsonElement.DeepEquals(beforeProp, prop.Value))
                        {
                            sb.AppendLine($"  ▸ {prop.Name}: \"{FormatValue(beforeProp)}\" → \"{FormatValue(prop.Value)}\"");
                            coThayDoi = true;
                        }
                    }
                }

                if (!coThayDoi)
                {
                    sb.AppendLine("⚠️ Không có thay đổi nào");
                }

                return sb.ToString();
            }

            sb.AppendLine("⚠️ Không rõ loại thao tác");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"❌ Lỗi phân tích JSON: {ex.Message}");
        }

        return sb.ToString();
    }
    private static string FormatValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "null",
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => value.ToString()
        };
    }

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
    private static string GetTenDoiTuong(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "(không rõ đối tượng)";
        var match = Regex.Match(path.ToLower(), @"\/?api\/?([a-zA-Z0-9]+)");
        if (!match.Success) return "(không rõ đối tượng)";
        var key = match.Groups[1].Value.ToLower();

        return TuDien._tableFriendlyNames.TryGetValue(key, out var name) ? name : key;
    }
    private static string? LayTenChinh(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Ưu tiên lấy trong afterData nếu có (cho thêm/sửa)
            if (root.TryGetProperty("afterData", out var after) && after.ValueKind == JsonValueKind.Object)
                return LayTenTuObject(after);

            // Nếu là xoá thì thử from beforeData
            if (root.TryGetProperty("beforeData", out var before) && before.ValueKind == JsonValueKind.Object)
                return LayTenTuObject(before);

            // Nếu không có thì thử toàn bộ object (fallback)
            return LayTenTuObject(root);
        }
        catch
        {
            return null;
        }
    }
    private static string? LayTenTuObject(JsonElement element)
    {
        string[] fields = { "ten", "Ten", "title", "Title", "name", "Name" };

        foreach (var field in fields)
        {
            if (element.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        return null;
    }

}
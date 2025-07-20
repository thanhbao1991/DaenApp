using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;

namespace TraSuaApp.Shared.Helpers
{
    public static class LogHelper
    {
        /// <summary>
        /// Sinh phần chi tiết (before/after) của log, bỏ marker truncated, không ném exception.
        /// </summary>
        public static string ChiTietLog(LogDto log)
        {
            var sb = new StringBuilder();

            // Lấy raw response, cắt marker nếu có
            var raw = log.ResponseBodyShort ?? "";
            const string truncMarker = "... [truncated]";
            if (raw.EndsWith(truncMarker, StringComparison.Ordinal))
                raw = raw.Substring(0, raw.Length - truncMarker.Length);

            try
            {
                // Parse JSON sau khi đã loại bỏ marker
                using var json = JsonDocument.Parse(raw);
                var root = json.RootElement;

                bool hasBefore = root.TryGetProperty("beforeData", out var beforeData);
                bool hasAfter = root.TryGetProperty("afterData", out var afterData);

                if (!hasBefore && !hasAfter)
                {
                    sb.AppendLine("⚠️ (không có dữ liệu trước/sau)");
                    return sb.ToString();
                }

                // XÓA
                if (hasBefore && (!hasAfter || afterData.ValueKind == JsonValueKind.Null))
                {
                    sb.AppendLine("✘ Đã xoá bản ghi:");
                    foreach (var prop in beforeData.EnumerateObject())
                        sb.AppendLine($"  ▸ {prop.Name}: {FormatValue(prop.Value)}");
                    return sb.ToString();
                }

                // THÊM
                if ((!hasBefore || beforeData.ValueKind == JsonValueKind.Null)
                    && hasAfter && afterData.ValueKind != JsonValueKind.Null)
                {
                    sb.AppendLine("+ Đã thêm bản ghi:");
                    foreach (var prop in afterData.EnumerateObject())
                        sb.AppendLine($"  ▸ {prop.Name}: {FormatValue(prop.Value)}");
                    return sb.ToString();
                }

                // SỬA
                if (hasBefore && hasAfter
                    && beforeData.ValueKind != JsonValueKind.Null
                    && afterData.ValueKind != JsonValueKind.Null)
                {
                    sb.AppendLine("✓ Cập nhật:");
                    bool changed = false;

                    foreach (var prop in afterData.EnumerateObject())
                    {
                        if (beforeData.TryGetProperty(prop.Name, out var beforeProp)
                            && !JsonElement.DeepEquals(beforeProp, prop.Value))
                        {
                            sb.AppendLine(
                              $"  ▸ {prop.Name}: “{FormatValue(beforeProp)}” → “{FormatValue(prop.Value)}”");
                            changed = true;
                        }
                    }

                    if (!changed)
                        sb.AppendLine("⚠️ Không có thay đổi");

                    return sb.ToString();
                }

                sb.AppendLine("⚠️ Không xác định thao tác");
            }
            catch (JsonException)
            {
                // Nếu parse lỗi, in raw để debug
                sb.Clear();
                sb.AppendLine("❌ Không parse được JSON, in raw:");
                sb.AppendLine(raw);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Tạo câu tóm tắt ngắn gọn (MessageBox-friendly).
        /// Ví dụ: "Nguyễn Văn A đã cập nhật Topping: 33334"
        /// </summary>
        public static string RutGonLog(LogDto log)
        {
            var entityName = GetTenDoiTuong(log.Path);
            var mainValue = LayTenChinh(log.ResponseBodyShort ?? log.RequestBodyShort);
            var action = log.Method?.ToUpper() switch
            {
                "POST" => "thêm",
                "PUT" => "cập nhật",
                "DELETE" => "xoá",
                _ => "thao tác"
            };

            var result = $"{log.UserName} đã {action} {entityName.ToLower()}";
            if (!string.IsNullOrEmpty(mainValue))
                result += $": {mainValue}";
            return result;
        }

        private static string GetTenDoiTuong(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "(không rõ)";
            var m = Regex.Match(path.ToLower(), @"\/?api\/?([a-z0-9]+)");
            if (!m.Success) return "(không rõ)";
            var key = m.Groups[1].Value;
            return TuDien._tableFriendlyNames.TryGetValue(key, out var n) ? n : key;
        }

        private static string? LayTenChinh(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("afterData", out var after) && after.ValueKind == JsonValueKind.Object)
                    return LayTenTuObject(after);
                if (root.TryGetProperty("beforeData", out var before) && before.ValueKind == JsonValueKind.Object)
                    return LayTenTuObject(before);
                return LayTenTuObject(root);
            }
            catch { return null; }
        }

        private static string? LayTenTuObject(JsonElement el)
        {
            foreach (var field in new[] { "ten", "title", "name" })
            {
                if (el.TryGetProperty(field, out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString();
            }
            return null;
        }

        private static string FormatValue(JsonElement v) => v.ValueKind switch
        {
            JsonValueKind.String => v.GetString() ?? "",
            JsonValueKind.Number => v.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => v.ToString()
        };
    }
}
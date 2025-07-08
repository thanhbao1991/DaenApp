using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views;

public partial class LogList : Window
{
    private List<LogDto> _all = new();
    private readonly WpfErrorHandler _errorHandler = new();

    public LogList()
    {
        InitializeComponent();
        _ = LoadAsync();
        this.PreviewKeyDown += LogList_PreviewKeyDown;
    }

    private void LogList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            ReloadButton_Click(null!, null!);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CloseButton_Click(null!, null!);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (LogDataGrid.SelectedItem is LogDto selected)
            {
                ShowLogDetailPopup(selected);
                e.Handled = true;
            }
        }
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var response = await ApiClient.GetAsync($"/api/logs");

            var result = await response.Content.ReadFromJsonAsync<Result<PagedResultDto<LogDto>>>();
            if (result?.IsSuccess != true)
                throw new Exception(result?.Message ?? "Không thể tải log.");

            var paged = result.Data;
            if (paged?.Items != null)
            {
                _all = paged.Items.OrderByDescending(x => x.ThoiGian).ToList();
                ApplySearch();
            }
        }
        catch (Exception ex)
        {
            _errorHandler.Handle(ex, "LoadAsync");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void ApplySearch()
    {
        var keyword = SearchTextBox.Text.Trim().ToLower();

        var filtered = _all
            .Where(x =>
                (x.Path ?? "").ToLower().Contains(keyword) ||
                (x.UserName ?? "").ToLower().Contains(keyword) ||
                (x.Method ?? "").ToLower().Contains(keyword))
            .ToList();

        for (int i = 0; i < filtered.Count; i++)
        {
            filtered[i].STT = i + 1;
            filtered[i].Message = TryGetMessage(filtered[i].ResponseBodyShort);
            filtered[i].TenDoiTuongChinh = GetTenDoiTuongChinh(filtered[i].RequestBodyShort, filtered[i].ResponseBodyShort);
        }

        LogDataGrid.ItemsSource = filtered;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplySearch();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void LogDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var row = ItemsControl.ContainerFromElement(LogDataGrid, e.OriginalSource as DependencyObject) as DataGridRow;
        if (row == null) return;

        if (row.Item is LogDto selected)
        {
            var response = await ApiClient.GetAsync($"/api/logs/{selected.Id}");
            var result = await response.Content.ReadFromJsonAsync<Result<LogDto>>();

            if (result?.IsSuccess != true)
            {
                MessageBox.Show(result?.Message ?? "Không thể tải chi tiết log.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (result.Data != null)
                ShowLogDetailPopup(result.Data);
        }
    }

    public static string? GetTenDoiTuongChinh(string? requestBody, string? responseBody)
    {
        var ten = TryGetTen(requestBody);
        if (!string.IsNullOrWhiteSpace(ten)) return ten;

        ten = TryGetTen(responseBody);
        return ten;
    }

    private static string? TryGetTen(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            var obj = JObject.Parse(json);
            var tenProps = new[] { "ten", "Ten", "tenDangNhap" };

            foreach (var prop in tenProps)
            {
                if (obj.TryGetValue(prop, StringComparison.OrdinalIgnoreCase, out var token))
                {
                    var val = token?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGetMessage(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            var obj = JObject.Parse(json);
            if (obj.TryGetValue("message", out var token))
            {
                var msg = token?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(msg))
                    return msg;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private void ShowLogDetailPopup(LogDto log)
    {
        var sb = new StringBuilder();

        string action = log.Method switch
        {
            "POST" => "➕ Thêm mới",
            "PUT" => "✏️ Chỉnh sửa",
            "DELETE" => "🟟️ Xoá",
            _ => $"{log.Method}"
        };

        var ignoredFields = new[]
        {
            "id", "idOld", "idNguoiTao", "idNguoiSua", "ngayTao", "ngaySua",
            "nguoiTao", "nguoiSua", "stt", "tenNormalized"
        };

        string? tenChinh = null;

        try
        {
            if (log.Method == "POST")
            {
                var doc = JsonDocument.Parse(log.RequestBodyShort ?? "{}");
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (!ignoredFields.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        var label = FormatFieldName(prop.Name);
                        var value = prop.Value.ToString();
                        if (tenChinh == null && IsTenChinhField(prop.Name)) tenChinh = value;
                        sb.AppendLine($"• {label}: {value}");
                    }
                }
            }
            else if (log.Method == "DELETE")
            {
                var id = TryExtractIdFromPath(log.Path);
                var beforeLog = _all
                    .Where(x => x.Path.Contains(id, StringComparison.OrdinalIgnoreCase)
                                && (x.Method == "POST" || x.Method == "PUT")
                                && x.ThoiGian < log.ThoiGian)
                    .OrderByDescending(x => x.ThoiGian)
                    .FirstOrDefault();

                if (beforeLog != null)
                {
                    var doc = JsonDocument.Parse(beforeLog.RequestBodyShort ?? "{}");
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (!ignoredFields.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                        {
                            var label = FormatFieldName(prop.Name);
                            var value = prop.Value.ToString();
                            if (tenChinh == null && IsTenChinhField(prop.Name)) tenChinh = value;
                            sb.AppendLine($"• {label}: {value}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("⚠️ Không tìm thấy log cũ để truy vết thông tin đã xoá.");
                }
            }
            else if (log.Method == "PUT")
            {
                var id = TryExtractIdFromPath(log.Path);
                var oldLog = _all
                    .Where(x => x.Path.Contains(id, StringComparison.OrdinalIgnoreCase)
                                && (x.Method == "POST" || x.Method == "PUT")
                                && x.ThoiGian < log.ThoiGian)
                    .OrderByDescending(x => x.ThoiGian)
                    .FirstOrDefault();

                var beforeDict = new Dictionary<string, string>();
                if (oldLog != null)
                {
                    var beforeDoc = JsonDocument.Parse(oldLog.RequestBodyShort ?? "{}");
                    beforeDict = beforeDoc.RootElement.EnumerateObject()
                        .Where(p => !ignoredFields.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                        .ToDictionary(p => p.Name, p => p.Value.ToString());
                }

                var afterDoc = JsonDocument.Parse(log.RequestBodyShort ?? "{}");
                var afterDict = afterDoc.RootElement.EnumerateObject()
                    .Where(p => !ignoredFields.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                    .ToDictionary(p => p.Name, p => p.Value.ToString());

                foreach (var key in afterDict.Keys)
                {
                    beforeDict.TryGetValue(key, out var oldVal);
                    var newVal = afterDict[key];

                    if (key.Contains("ten", StringComparison.OrdinalIgnoreCase) && tenChinh == null)
                        tenChinh = newVal;

                    if (oldVal != newVal)
                    {
                        sb.AppendLine($"• {FormatFieldName(key)}: \"{oldVal}\" → \"{newVal}\"");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("⚠️ Không thể phân tích nội dung.");
            sb.AppendLine(ex.Message);
        }

        if (!string.IsNullOrWhiteSpace(log.ExceptionMessage))
        {
            sb.AppendLine();
            sb.AppendLine("❗ Lỗi hệ thống:");
            sb.AppendLine(log.ExceptionMessage);
        }

        var title = $"{action}" + (tenChinh != null ? $": {tenChinh}" : "");
        MessageBox.Show(sb.ToString(), title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string TryExtractIdFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        var parts = path.Split('/');
        return parts.LastOrDefault(p => Guid.TryParse(p, out _)) ?? "";
    }

    private string FormatFieldName(string fieldName)
    {
        var result = Regex.Replace(fieldName, "(?<!^)([A-Z])", " $1");
        return char.ToUpper(result[0]) + result.Substring(1);
    }

    private bool IsTenChinhField(string name)
    {
        var tenProps = new[] {
            "ten", "tenSanPham", "tenNhom", "tenKhachHang", "tenTopping", "tenNguyenLieu", "tenHienThi"
        };
        return tenProps.Contains(name, StringComparer.OrdinalIgnoreCase);
    }
}
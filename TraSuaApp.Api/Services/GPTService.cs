using System.Text;
using System.Text.Json;

public class TimeStat
{
    public DateTime Date { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; } // thêm phút
    public int SoDon { get; set; }
    public decimal DoanhThu { get; set; }
    public string Thu { get; set; } // thêm thứ

}
public class GPTService
{
    private static readonly string _apiKey = "AIzaSyBbxxBryfwU7sSwNCLVzLHkBph_nnRTuEg"; // 🟟 Thay bằng API Key của Google Gemini

    public static async Task<string> DuDoanGioDongKhachAsync(List<TimeStat> data)
    {
        var grouped = data
            .GroupBy(x => new { x.Thu, x.Hour, x.Minute })
            .OrderBy(g => g.Key.Thu)
            .ThenBy(g => g.Key.Hour)
            .ThenBy(g => g.Key.Minute)
            .Select(g => new
            {
                TimeLabel = $"{g.Key.Thu} {g.Key.Hour:00}:{g.Key.Minute:00}",
                TotalOrders = g.Sum(x => x.SoDon),
                TotalRevenue = g.Sum(x => x.DoanhThu),
                AvgPerOrder = g.Sum(x => x.DoanhThu) / Math.Max(g.Sum(x => x.SoDon), 1)
            });

        var lines = grouped
            .OrderByDescending(x => x.TotalOrders)
            //.Take(30) // hoặc tất cả nếu ngắn
            .Select(x => $"{x.TimeLabel}: {x.TotalOrders} đơn, {x.TotalRevenue:n0}đ (TB: {x.AvgPerOrder:n0}đ/đơn)");

        var prompt = @"Dưới đây là dữ liệu thống kê trong 2 tháng gần nhất về số đơn và doanh thu, chia theo thứ trong tuần và khung 10 phút:

" + string.Join("\n", lines) + @"

Mỗi dòng có định dạng: ""[Thứ] [giờ:phút]: [số đơn] đơn, [doanh thu]đ (TB: [doanh thu trung bình/đơn]đ)""

Hôm nay là " + GetThuVietnamese(DateTime.Today.DayOfWeek) + @".

Bây giờ là " + DateTime.Now.ToString("HH:mm") + @".  
Dựa trên dữ liệu trên, hãy dự đoán **khung giờ còn lại nào trong hôm nay** có khả năng đông khách nhất.

- Tóm tắt ngay đầu tiên: ghi các khung giờ dự đoán
- Sau đó giải thích ngắn gọn dựa theo xu hướng từ dữ liệu
- Trình bày cách dễ hiều nhất.";
        using var client = new HttpClient();

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=" + _apiKey;
        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Gemini API Error: " + errorText);
            return "Không dự đoán được (lỗi kết nối Gemini).";
        }

        var result = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(result);

        // Trích xuất nội dung phản hồi
        var contentText = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return contentText ?? "Không có phản hồi từ Gemini.";
    }

    private static string GetThuVietnamese(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "Thứ 2",
            DayOfWeek.Tuesday => "Thứ 3",
            DayOfWeek.Wednesday => "Thứ 4",
            DayOfWeek.Thursday => "Thứ 5",
            DayOfWeek.Friday => "Thứ 6",
            DayOfWeek.Saturday => "Thứ 7",
            DayOfWeek.Sunday => "Chủ nhật",
            _ => ""
        };
    }
}
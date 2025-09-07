using System.Text.Json;
using OpenAI.Chat;

namespace TraSuaApp.WpfClient.Services
{
    public class QuickOrderDto
    {
        public string TenMon { get; set; } = string.Empty;
        public string BienThe { get; set; } = "Size chuẩn"; // default
        public int SoLuong { get; set; } = 1;              // default
    }
    public class QuickOrderService
    {
        private readonly ChatClient _chatClient;

        public QuickOrderService(string apiKey)
        {
            _chatClient = new ChatClient("gpt-4o-mini", apiKey);
        }

        public async Task<List<QuickOrderDto>> ParseQuickOrderAsync(string input, IEnumerable<string> menu)
        {
            string systemPrompt = @"
Bạn là hệ thống POS cho quán trà sữa.
Nhiệm vụ: Chuẩn hoá text order tự do thành JSON theo MENU.

Quy tắc:
- Người dùng có thể viết sai chính tả hoặc viết tắt.
- Chỉ được chọn tên sản phẩm trong MENU (TenMon) đúng y như trong danh sách, không tự tạo mới.
- Số lượng có thể được ghi ở **đầu dòng** (ví dụ: '2 trà sữa') hoặc ở **cuối dòng** (ví dụ: 'trà sữa 2 ly').
- Nếu có số lượng → lấy đúng số đó, nếu không có số lượng → SoLuong = 1.
- Nếu người dùng ghi size:
   + 'M', 'chuẩn', 'medium', 'vừa' → BienThe = 'Size chuẩn'
   + 'L', 'large', 'lon', 'bự' → BienThe = 'Size L'
- Nếu không ghi size → BienThe = 'Size chuẩn' (mặc định).
- Output chỉ là JSON array, không thêm chữ nào khác:
[
  { ""TenMon"": ""<Tên sản phẩm trong MENU>"", ""BienThe"": ""Size chuẩn hoặc Size L"", ""SoLuong"": <số nguyên> }
]
";

            string userPrompt = "MENU (hãy chọn đúng y tên trong đây):\n- "
      + string.Join("\n- ", menu)
      + "\n\nINPUT:\n" + input;
            var result = await _chatClient.CompleteChatAsync(new ChatMessage[]
      {
    new SystemChatMessage(systemPrompt),
    new UserChatMessage(userPrompt)
      });

            var completion = result.Value;
            var raw = completion.Content[0].Text;
            // 🟟 Làm sạch JSON trả về
            raw = raw.Trim();
            if (raw.StartsWith("```"))
            {
                int first = raw.IndexOf('\n');
                int last = raw.LastIndexOf("```");
                if (first >= 0 && last > first)
                    raw = raw.Substring(first, last - first).Trim();
            }
            try
            {
                return JsonSerializer.Deserialize<List<QuickOrderDto>>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            catch
            {
                return new();
            }
        }
    }
}
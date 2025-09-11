using System.Text.Json;
using OpenAI.Chat;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.WpfClient.Services
{
    public class QuickOrderDto
    {
        public string TenMon { get; set; } = string.Empty;     // phải khớp tên sản phẩm trong DB
        public string BienThe { get; set; } = "Size chuẩn";    // ví dụ: Size chuẩn | Size L
        public int SoLuong { get; set; } = 1;                  // mặc định 1
        public string NoteText { get; set; } = "";             // ghi chú thêm: ít ngọt, ít đá, thêm trân châu...
    }

    public class QuickOrderService
    {
        private readonly ChatClient _chatClient;

        // ✅ Build menu đúng 1 lần khi app chạy (thread-safe)
        private static readonly Lazy<string> _menuText = new(() => BuildMenuForGpt(), isThreadSafe: true);

        public QuickOrderService(string apiKey)
        {
            _chatClient = new ChatClient("gpt-4o-mini", apiKey);
        }

        private static string BuildMenuForGpt()
        {
            // lấy từ Dashboard/AppProviders như bạn đang dùng
            var sanPhams = AppProviders.SanPhams.Items
                .Where(x => !x.NgungBan)
                .OrderBy(x => x.Ten)
                .ToList();

            var lines = new List<string>();
            foreach (var sp in sanPhams)
            {
                if (sp.BienThe == null || sp.BienThe.Count == 0) continue;

                foreach (var bt in sp.BienThe.OrderBy(b => b.TenBienThe))
                {
                    var tenSize = string.IsNullOrWhiteSpace(bt.TenBienThe) ? "Size chuẩn" : bt.TenBienThe;
                    var gia = (long)Math.Round(bt.GiaBan);
                    if (gia <= 0) continue; // bỏ biến thể chưa set giá để GPT khỏi suy sai

                    lines.Add($"{sp.Ten} | {tenSize} | {gia}");
                }
            }

            return string.Join("\n", lines);
        }

        public async Task<List<QuickOrderDto>> ParseQuickOrderAsync(string input)
        {
            var menuText = _menuText.Value; // dùng cache

            const string systemPrompt = @"
Bạn là hệ thống POS. Chuẩn hóa INPUT thành JSON các món có trong MENU.

QUY TẮC MAP:
- So khớp gần đúng, bỏ dấu, viết thường, chấp nhận lỗi chính tả nhẹ.
- Dùng giá trong dòng để suy ra Size nếu có:
  + Nếu giá trùng đúng với 1 biến thể của món → chọn biến thể đó.
- Cho phép đồng nghĩa:
  + ""kem trứng"" ≈ ""trứng nướng""
  + ""tcđđ"" ≈ ""trân châu đường đen""
  + ""olong"" ≈ ""oolong"" ≈ ""ô long""
- Nếu tên gần nhất khớp với 1 món trong MENU → vẫn chọn món đó (không được bỏ sót chỉ vì khác chính tả).
- Mỗi dòng có số lượng → phải sinh đúng 1 item.
- Nếu không suy ra được size mà dòng có giá → dùng giá để chọn size. Nếu vẫn không rõ → Size Chuẩn.
- Cuối cùng, người dùng có thể nhập nhiều sản phẩm vào một dòng, có thể nhập lung tung, không có quy tắc gì, lúc đó hãy tận dụng trí thông minh của bạn để suy luận.
ĐỊNH DẠNG JSON CHÍNH XÁC:
[
  {""TenMon"":""<Tên trong MENU>"", ""BienThe"":""Size Chuẩn|Size L"", ""SoLuong"":<int>, ""NoteText"":""<chuỗi tự do>""}
]

VÍ DỤ BẮT BUỘC (làm theo đúng):
Input line: ""1 bạc xỉu kem trứng 25k""
→ {""TenMon"":""Bạc Xỉu Trứng Nướng"",""BienThe"":""Size Chuẩn"",""SoLuong"":1,""NoteText"":""""}

Input line: ""1 bạc xỉu kem trứng 30k""
→ {""TenMon"":""Bạc Xỉu Trứng Nướng"",""BienThe"":""Size L"",""SoLuong"":1,""NoteText"":""""}
";

            string userPrompt = $@"
MENU (mỗi dòng: Tên món | Size | Giá-đồng):
{menuText}

INPUT (nguyên văn):
{input}
";

            var opts = new ChatCompletionOptions
            {
                Temperature = 0f
                // Nếu SDK bạn hỗ trợ JSON mode thì bật:
                // ResponseFormat = ChatResponseFormat.Json
            };

            var result = await _chatClient.CompleteChatAsync(new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            }, opts);

            var raw = result.Value.Content[0].Text?.Trim() ?? "[]";

            // GPT đôi khi bọc ```json ... ```
            if (raw.StartsWith("```"))
            {
                int first = raw.IndexOf('\n');
                int last = raw.LastIndexOf("```");
                if (first >= 0 && last > first)
                    raw = raw.Substring(first, last - first).Trim();
            }

            List<QuickOrderDto> list;
            try
            {
                list = JsonSerializer.Deserialize<List<QuickOrderDto>>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            catch (Exception ex)
            {
                throw new Exception($"Không parse được JSON từ GPT: {ex.Message}\nRaw: {raw}");
            }

            // defaults an toàn
            foreach (var it in list)
            {
                if (it.SoLuong <= 0) it.SoLuong = 1;
                if (string.IsNullOrWhiteSpace(it.BienThe)) it.BienThe = "Size chuẩn";
                it.NoteText ??= "";
            }

            return list;
        }

        // QuickOrderService.cs (thêm vào dưới ParseQuickOrderAsync)
        private static SanPhamBienTheDto? ChonBienTheFallback(SanPhamDto sp, string? bienTheTen)
        {
            var match = sp.BienThe.FirstOrDefault(bt =>
                bt.TenBienThe.Equals(bienTheTen ?? "", StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;

            if (sp.BienThe.Count == 1) return sp.BienThe[0];

            var macDinh = sp.BienThe.FirstOrDefault(bt => bt.MacDinh);
            if (macDinh != null) return macDinh;

            var sizeChuan = sp.BienThe.FirstOrDefault(bt =>
                bt.TenBienThe.Equals("Size chuẩn", StringComparison.OrdinalIgnoreCase));
            if (sizeChuan != null) return sizeChuan;

            return sp.BienThe.FirstOrDefault();
        }

        public async Task<List<ChiTietHoaDonDto>> ParseToChiTietAsync(string input)
        {
            var items = await ParseQuickOrderAsync(input); // giữ nguyên logic gọi GPT
            var sanPhams = AppProviders.SanPhams.Items.Where(x => !x.NgungBan).ToList();
            var bienTheAll = sanPhams.SelectMany(x => x.BienThe).ToList();

            var list = new List<ChiTietHoaDonDto>();
            foreach (var it in items)
            {
                var sp = sanPhams.FirstOrDefault(x =>
                    x.Ten.Equals(it.TenMon, StringComparison.OrdinalIgnoreCase));
                if (sp == null) continue;

                var bt = ChonBienTheFallback(sp, it.BienThe);
                if (bt == null) continue;

                list.Add(new ChiTietHoaDonDto
                {
                    Id = Guid.NewGuid(),
                    SanPhamIdBienThe = bt.Id,
                    TenSanPham = sp.Ten,
                    TenBienThe = bt.TenBienThe,
                    DonGia = bt.GiaBan,
                    SoLuong = it.SoLuong > 0 ? it.SoLuong : 1,
                    Stt = 0, // lát nữa đánh lại
                    BienTheList = bienTheAll.Where(x => x.SanPhamId == sp.Id).ToList(),
                    ToppingDtos = new List<ToppingDto>(),
                    NoteText = it.NoteText ?? ""
                });
            }

            // đánh STT
            int stt = 1;
            foreach (var ct in list) ct.Stt = stt++;

            return list;
        }

        public async Task<HoaDonDto> BuildHoaDonFromQuickAsync(string input)
        {
            var chiTiets = await ParseToChiTietAsync(input);
            return new HoaDonDto
            {
                Id = Guid.Empty,
                Ngay = DateTime.Now.Date,
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                ChiTietHoaDons = chiTiets,
                ChiTietHoaDonToppings = new List<ChiTietHoaDonToppingDto>(),
                ChiTietHoaDonVouchers = new List<ChiTietHoaDonVoucherDto>()
                // giữ các field khác mặc định; bạn có thể set PhanLoai/TenBan… ở chỗ gọi
            };
        }

    }
}
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenAI.Chat;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.WpfClient.Services
{
    public class QuickOrderDto
    {
        public Guid Id { get; set; } = Guid.Empty; // Id sản phẩm trong MENU
        public int SoLuong { get; set; } = 1;
        public string NoteText { get; set; } = "";
    }

    public class QuickOrderEngine
    {
        private readonly ChatClient _chatClient;



        public QuickOrderEngine(string apiKey)
        {
            _chatClient = new ChatClient("gpt-4o", apiKey);
        }

        // --- Tiền xử lý: mở rộng viết tắt/phổ biến, giữ \n ---
        private static readonly (string pattern, string repl)[] PreFilters = new[]
        {
            (@"(?<!\w)(nc)(?!\w)", "nước"),
            (@"(?<!\w)(sto)(?!\w)", "sinh tố"),
            (@"(?<!\w)(ts)(?!\w)", "trà sữa"),
            (@"(?<!\w)(tcdd|tcđđ|tcdđ)(?!\w)", "trân châu đường đen"),
            (@"(?<!\w)(nuoc dua)(?!\w)", "dừa tươi"),
            (@"(?<!\w)(nuoc cam)(?!\w)", "ép cam"),
            (@"(?<!\w)(cf|cafe)(?!\w)", "cà phê"),
            (@"(?<!\w)(oolong)(?!\w)", "ô long"),
            (@"(?<!\w)(ly)(?!\w)", ""),
            (@"(?<!\w)(kem trứng)(?!\w)", "trứng"),
            (@"(?<!\w)(kem muối)(?!\w)", "muoối"),
            (@"(?<!\w)(enter)(?!\w)", ""),
        };

        public static string PreFilterForModel(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.ToLower();           // chuẩn hoá: lower, bỏ dấu, bỏ ký tự rác cơ bản
            var x = " " + s.Trim() + " ";

            x = Regex.Replace(x, @"(?<=\d)(?=\p{L})|(?<=\p{L})(?=\d)", " ");

            foreach (var (pattern, repl) in PreFilters)
                x = Regex.Replace(x, pattern, repl, RegexOptions.IgnoreCase);

            // giữ xuống dòng; chỉ gộp space/tab
            x = Regex.Replace(x, @"[ \t]+", " ");    // gộp space/tab
            x = Regex.Replace(x, @"\n{2,}", "\n");   // nhiều dòng trống -> 1
            return x.Trim();
        }


        private static string BuildLinesBlock(string filteredInput, out int validCount)
        {
            var lines = filteredInput.Split('\n').Select(l => l.Trim()).ToList();
            var kept = new List<string>();
            foreach (var l in lines)
            {
                if (string.IsNullOrWhiteSpace(l)) continue;
                kept.Add(l);
            }

            validCount = kept.Count;
            // Đánh số để neo thứ tự, 1 dòng -> 1 item
            var numbered = kept.Select((val, idx) => $"{idx + 1}) {val}");
            return string.Join("\n", numbered);
        }

        public async Task<List<QuickOrderDto>> ParseQuickOrderAsync(string input)
        {
            var filteredInput = PreFilterForModel(input ?? "");
            var menuText = AppProviders.QuickOrderMenu;   // 🟟 lấy menu từ AppProviders
            var linesBlock = BuildLinesBlock(filteredInput, out var lineCount);

            const string systemPrompt = @"
Bạn là hệ thống POS. Chỉ trả về DUY NHẤT một mảng JSON hợp lệ, không có text nào khác.

Bạn sẽ nhận 2 phần:
1) MENU: danh sách sản phẩm dạng ""Id<TAB>Tên"" (tên đã được viết thường/không dấu).
2) LINES: danh sách các dòng người dùng (đã lọc cơ bản), mỗi dòng tương ứng 1 món.

NHIỆM VỤ (BẮT BUỘC):
- Xử lý TỪNG DÒNG trong LINES, giữ nguyên thứ tự. 
- Nếu dòng KHÔNG khớp sản phẩm nào trong MENU thì BỎ QUA, không tạo phần tử.
- Nếu khớp: tạo **đúng 1** phần tử JSON.
- Chọn sản phẩm trong MENU có TÊN KHỚP NHẤT với nội dung dòng. KHÔNG tạo tên mới.
- Ưu tiên khớp cụm dài/đủ nghĩa:  
  ví dụ: ""ep cam it da"" → chọn ""ep cam nguyen chat"" (nếu có), còn ""cam ca rot"" → ""ep cam ca rot"".
- Hiểu đồng nghĩa/phổ biến: ""nuoc cam"" ≈ ""ep cam"", ""den da"" ≈ ""ca phe den da"", ""ly/coc"" chỉ là đơn vị.
- Trích số lượng từ dòng (""x2"", ""2 ly"", ""1"", ...). Mặc định 1 nếu không ghi.
- Phần ghi chú như ""it da"", ""it duong"", ""khong tran chau"", ""xin them bich da"", ""nong/da""... → đưa vào NoteText.

ĐỊNH DẠNG PHẦN TỬ:
{
  ""Id"": ""Id từ MENU"",
  ""SoLuong"": số nguyên >= 1,
  ""NoteText"": ""ghi chú, có thể rỗng""
}
";

            string userPrompt = $@"
MENU:
{menuText}

LINES:
{linesBlock}
";

            var result = await _chatClient.CompleteChatAsync(new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            }, new ChatCompletionOptions { Temperature = 0 });

            var raw = result.Value.Content[0].Text?.Trim() ?? "[]";
            if (raw.StartsWith("```"))
            {
                int first = raw.IndexOf('\n');
                int last = raw.LastIndexOf("```");
                if (first >= 0 && last > first) raw = raw.Substring(first, last - first).Trim();
            }

            // Debug lên Discord để dễ so kết quả
            var noiDung = $"systemPrompt:\n{systemPrompt}\n" +
                          $"userPrompt:\n{userPrompt}\n" +
                          $"result:\n{raw}";
            await DiscordService.SendAsync(DiscordEventType.Admin, noiDung);

            var list = JsonSerializer.Deserialize<List<QuickOrderDto>>(raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            // Chỉnh dữ liệu an toàn
            foreach (var it in list)
            {
                if (it.SoLuong <= 0) it.SoLuong = 1;
                it.NoteText ??= "";
            }

            return list;
        }

        private static SanPhamBienTheDto? ChonBienTheDefault(SanPhamDto sp)
        {
            if (sp.BienThe == null || sp.BienThe.Count == 0) return null;
            var macDinh = sp.BienThe.FirstOrDefault(bt => bt.MacDinh);
            if (macDinh != null) return macDinh;
            return sp.BienThe.First(); // fallback: biến thể đầu tiên
        }

        public async Task<ObservableCollection<ChiTietHoaDonDto>> MapToChiTietAsync(string input)
        {
            var items = await ParseQuickOrderAsync(input);
            var sanPhams = AppProviders.SanPhams.Items.Where(x => !x.NgungBan).ToList();
            var bienTheAll = sanPhams.SelectMany(x => x.BienThe).ToList();

            var list = new ObservableCollection<ChiTietHoaDonDto>();
            var baseTime = DateTime.Now;

            foreach (var it in items)
            {
                var sp = sanPhams.SingleOrDefault(x => x.Id == it.Id);
                if (sp == null) continue;

                var bt = ChonBienTheDefault(sp);
                if (bt == null) continue;

                list.Add(new ChiTietHoaDonDto
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = baseTime,
                    LastModified = baseTime,

                    SanPhamIdBienThe = bt.Id,
                    TenSanPham = sp.Ten,
                    TenBienThe = bt.TenBienThe,
                    DonGia = bt.GiaBan,
                    SoLuong = it.SoLuong,
                    Stt = 0,
                    BienTheList = bienTheAll.Where(x => x.SanPhamId == sp.Id).ToList(),
                    ToppingDtos = new List<ToppingDto>(),
                    NoteText = it.NoteText ?? ""
                });
            }

            int stt = 1;
            foreach (var ct in list) ct.Stt = stt++;

            return list;
        }
    }
}
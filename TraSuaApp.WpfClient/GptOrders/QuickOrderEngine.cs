using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenAI.Chat;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.WpfClient.Ordering
{
    public class QuickOrderDto
    {
        public int Line { get; set; } = 0;               // NEW: 1-based theo LINES
        public Guid Id { get; set; } = Guid.Empty;       // Id sản phẩm trong MENU
        public int SoLuong { get; set; } = 1;
        public string NoteText { get; set; } = "";
    }

    public class QuickOrderEngine
    {
        private readonly ChatClient _chatClient;
        private readonly QuickOrderMemory _memory = QuickOrderMemory.Instance;

        public QuickOrderEngine(string apiKey)
        {
            //_chatClient = new ChatClient("gpt-4o", apiKey); //2.5
            _chatClient = new ChatClient("gpt-4.1", apiKey); //2.0
            //_chatClient = new ChatClient("gpt-4.1-mini", apiKey); //0.4
            //_chatClient = new ChatClient("gpt-4o-mini", apiKey); //0.15
        }

        // --- Tiền xử lý: mở rộng viết tắt/phổ biến, giữ \n ---
        private static readonly (string pattern, string repl)[] PreFilters = new[]
        {
            // =====================================================================
            // 🟟 NHÓM 1 — VIẾT TẮT / ĐỒNG NGHĨA PHỔ BIẾN
            // =====================================================================
            (@"(?<!\w)(cf|cafe)(?!\w)", "ca phe"),
            (@"(?<!\w)(nc)(?!\w)", "nuoc"),
            (@"(?<!\w)(sto)(?!\w)", "sinh to"),
            (@"(?<!\w)(st)(?!\w)", "sua tuoi"),
            (@"(?<!\w)(ts)(?!\w)", "tra sua"),
            (@"(?<!\w)(tcdd|tcđđ|tcdđ)(?!\w)", "tran chau duong den"),
            (@"(?<!\w)(nuoc dua)(?!\w)", "dua tuoi"),
            (@"(?<!\w)(nuoc cam)(?!\w)", "ep cam"),
            (@"(?<!\w)(oolong)(?!\w)", "olong"),

            // =====================================================================
            // 🟟 NHÓM 2 — CỤM GỢI Ý "SHIP / MANG VỀ"
            // =====================================================================
            (@"(?<!\w)(mv|ship|mang ve|ship nha|ship nhe|ship ne|mang ve nha)(?!\w)", "ship mv"),

            // =====================================================================
            // 🟟 NHÓM 3 — TỪ CẢM THÁN / FILLER KHÔNG MANG NGHĨA
            // =====================================================================
            (@"(?<!\w)(nha)(?!\w)", ""),
            (@"(?<!\w)(nhe)(?!\w)", ""),
            (@"(?<!\w)(di)(?!\w)", ""),
            (@"(?<!\w)(ha)(?!\w)", ""),
            (@"(?<!\w)(hen)(?!\w)", ""),
            (@"(?<!\w)(haiz)(?!\w)", ""),
            (@"(?<!\w)(na)(?!\w)", ""),
            (@"(?<!\w)(ne)(?!\w)", ""),
            (@"(?<!\w)(vay)(?!\w)", ""),
            (@"(?<!\w)(duoc)(?!\w)", ""),
            (@"(?<!\w)(nhe nha)(?!\w)", ""),
            (@"(?<!\w)(nhe nhe)(?!\w)", ""),
            (@"(?<!\w)(nhe a)(?!\w)", ""),
            (@"(?<!\w)(nhe e)(?!\w)", ""),
            (@"(?<!\w)(nhe c)(?!\w)", ""),
            (@"(?<!\w)(nha a)(?!\w)", ""),
            (@"(?<!\w)(nha e)(?!\w)", ""),
            (@"(?<!\w)(nha c)(?!\w)", ""),

            // =====================================================================
            // 🟟 NHÓM 4 — ĐƠN VỊ, KÝ HIỆU KHÔNG ẢNH HƯỞNG
            // =====================================================================
            (@"(?<!\w)(ly|coc|chai|chai nho|chai lon|ly nho|ly lon)(?!\w)", ""),
            (@"(?<!\w)(kem trung)(?!\w)", "trung"),
            (@"(?<!\w)(kem muoi)(?!\w)", "muoi"),
            (@"(?<!\w)(enter)(?!\w)", ""),

            // =====================================================================
            // 🟟 NHÓM 5 — TẠP ÂM KHÁC / NOISE CẦN LOẠI
            // =====================================================================
            (@"(?<!\w)(ok|oke|okela|okay)(?!\w)", ""),
            (@"(?<!\w)(thanks|thank|tks|tk|cam on|c ơn)(?!\w)", ""),
            (@"(?<!\w)(hihi|hehe|kkk|kk)(?!\w)", ""),
        };

        public static string PreFilterForModel(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.ToLower();
            var x = " " + s.Trim() + " ";

            x = Regex.Replace(x, @"(?<=\d)(?=\p{L})|(?<=\p{L})(?=\d)", " ");

            foreach (var (pattern, repl) in PreFilters)
                x = Regex.Replace(x, pattern, repl, RegexOptions.IgnoreCase);

            // giữ xuống dòng; chỉ gộp space/tab
            x = Regex.Replace(x, @"[ \t]+", " ");
            x = Regex.Replace(x, @"\n{2,}", "\n");
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
            var numbered = kept.Select((val, idx) => $"{idx + 1}) {val}");
            return string.Join("\n", numbered);
        }

        public async Task<List<QuickOrderDto>> ParseQuickOrderAsync(string input, string? shortMenu)
        {
            var filteredInput = PreFilterForModel(input ?? "");
            var menuText = StringHelper.NormalizeText(AppProviders.QuickOrderMenu);
            if (!string.IsNullOrWhiteSpace(shortMenu))
                shortMenu = StringHelper.NormalizeText(shortMenu);
            var linesBlock = StringHelper.NormalizeText(BuildLinesBlock(filteredInput, out var lineCount));

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

""Khi có SHORTLIST:
- Ưu tiên chọn trong SHORTLIST trước khi tìm trong MENU.
- Nếu dòng người dùng chỉ chứa một phần tên hoặc từ khóa đặc trưng của món trong SHORTLIST,
  vẫn coi là khớp và chọn món đó.
  Ví dụ:
    - '1 muoi' hoặc 'muoi' → 'ca phe muoi'
    - '1 phe' → 'ca phe sua da'
- Nếu nhiều món trong SHORTLIST đều có thể khớp, hãy **chọn món xuất hiện sớm hơn trong SHORTLIST**, 
  vì danh sách này đã được sắp xếp theo tần suất khách hàng hay gọi.
- Chỉ áp dụng lới lỏng này với SHORTLIST; không áp dụng cho toàn MENU.""

ĐỊNH DẠNG PHẦN TỬ:
{
  ""Line"": số nguyên (1-based, theo LINES đã đánh số),
  ""Id"": ""Id từ MENU"",
  ""SoLuong"": số nguyên >= 1,
  ""NoteText"": ""ghi chú, có thể rỗng""
}
";
            var systemWithMenu = systemPrompt + "\n\nMENU:\n" + menuText;

            var up = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(shortMenu))
            {
                up.AppendLine("SHORTLIST (Id<TAB>Tên) — ưu tiên chọn trong danh sách này; nếu không khớp, mới dùng MENU:");
                up.AppendLine(shortMenu);
                up.AppendLine();
            }
            up.AppendLine("LINES:");
            up.AppendLine(linesBlock);
            string userPrompt = up.ToString();

            var result = await _chatClient.CompleteChatAsync(new ChatMessage[]
            {
                new SystemChatMessage(systemWithMenu),
                new UserChatMessage(userPrompt)
            }, new ChatCompletionOptions { Temperature = 0 });

            var raw = result.Value.Content[0].Text?.Trim() ?? "[]";
            if (raw.StartsWith("```"))
            {
                int first = raw.IndexOf('\n');
                int last = raw.LastIndexOf("```");
                if (first >= 0 && last > first) raw = raw.Substring(first, last - first).Trim();
            }

            var noiDung =
                          $"userPrompt:\n{userPrompt}\n" +
                          $"result:\n{raw}" +
                          $"systemPrompt:\n{systemWithMenu}\n";

            await DiscordService.SendAsync(DiscordEventType.Admin, noiDung, "GPT.txt");

            var list = JsonSerializer.Deserialize<List<QuickOrderDto>>(raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            // Chỉnh dữ liệu an toàn
            foreach (var it in list)
            {
                if (it.SoLuong <= 0) it.SoLuong = 1;
                it.NoteText ??= "";
            }

            // 🟟 Học & báo cáo: dựa vào "Line" để biết dòng nào đã match
            var rawLines = filteredInput.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

            var matchedFlags = new bool[rawLines.Count];
            foreach (var it in list)
            {
                if (it.Line >= 1 && it.Line <= rawLines.Count)
                    matchedFlags[it.Line - 1] = true;
            }

            var newMisses = new List<string>();
            for (int i = 0; i < rawLines.Count; i++)
            {
                if (!matchedFlags[i])
                {
                    var ln = rawLines[i];
                    var isNew = _memory.MarkMiss(ln);
                    if (isNew) newMisses.Add(ln);
                }
            }

            if (newMisses.Count > 0)
            {
                var log = "**🟟 MISS mới (đã lưu để tự học):**\n" +
                          string.Join("\n", newMisses.Select((l, idx) => $"{idx + 1}. {l}"));
                await DiscordService.SendAsync(DiscordEventType.Admin, log);
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

        public async Task<ObservableCollection<ChiTietHoaDonDto>> MapToChiTietAsync(string input, string? shortMenu)
        {
            var items = await ParseQuickOrderAsync(input, shortMenu);
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
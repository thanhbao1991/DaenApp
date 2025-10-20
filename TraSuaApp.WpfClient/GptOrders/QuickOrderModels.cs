namespace TraSuaApp.WpfClient.Services
{
    public class QuickOrderDto
    {
        public Guid Id { get; set; } = Guid.Empty;
        public int SoLuong { get; set; } = 1;
        public string NoteText { get; set; } = "";
        public int? Line { get; set; }

        // 🟟 thêm giá bán GPT suy ra (nếu có)
        public decimal? Gia { get; set; }
    }
}
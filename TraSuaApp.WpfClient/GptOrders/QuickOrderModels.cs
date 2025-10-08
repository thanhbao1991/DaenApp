namespace TraSuaApp.WpfClient.Services
{
    public class QuickOrderDto
    {
        public Guid Id { get; set; } = Guid.Empty;
        public int SoLuong { get; set; } = 1;
        public string NoteText { get; set; } = "";
        public int? Line { get; set; }   // để học theo đúng dòng
    }

    public class QuickOrderNameDto
    {
        public string TenMon { get; set; } = string.Empty;
        public string BienThe { get; set; } = "Size Chuẩn"; // chuẩn hoá nhãn
        public int SoLuong { get; set; } = 1;
        public string NoteText { get; set; } = "";
        public int? Line { get; set; }
    }
}
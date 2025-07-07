namespace TraSuaApp.Shared.Dtos
{
    public class LogFilterDto
    {
        public string? UserName { get; set; }
        public string? Path { get; set; }
        public int? StatusCode { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
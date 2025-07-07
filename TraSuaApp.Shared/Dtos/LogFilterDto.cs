namespace TraSuaApp.Shared.Dtos
{
    public class LogDto
    {
        public Guid Id { get; set; }
        public DateTime ThoiGian { get; set; }
        public string? UserName { get; set; }
        public string? Method { get; set; }
        public string? Path { get; set; }
        public int StatusCode { get; set; }
        public long? DurationMs { get; set; }
    }
}
namespace TraSuaApp.Domain.Entities
{
    public class Log
    {
        public Guid Id { get; set; }
        public DateTime ThoiGian { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Method { get; set; }
        public string? Path { get; set; }
        public string? QueryString { get; set; }
        public string? RequestBody { get; set; }
        public int StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public string? IP { get; set; }
        public long? DurationMs { get; set; }
        public string? ExceptionMessage { get; set; }
    }
}
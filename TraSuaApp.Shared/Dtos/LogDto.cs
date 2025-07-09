namespace TraSuaApp.Shared.Dtos
{
    public class LogDto
    {
        public Guid Id { get; set; }
        public DateTime ThoiGian { get; set; }
        public string? Ip { get; set; }
        public Guid? EntityId { get; set; }

        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Method { get; set; }
        public string? KetQua { get; set; }

        public string? Path { get; set; }
        public int StatusCode { get; set; }
        public int STT { get; set; }
        public string? RequestBodyShort { get; set; }
        public string? ResponseBodyShort { get; set; }
        public long? DurationMs { get; set; }
    }

}
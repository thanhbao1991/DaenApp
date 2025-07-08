namespace TraSuaApp.Shared.Dtos
{
    public class LogDto
    {
        public Guid Id { get; set; }
        public DateTime ThoiGian { get; set; }

        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Message { get; set; }
        public string? TableReadable { get; set; }
        public string? TenDoiTuongChinh { get; set; }

        public string? Method { get; set; }
        public string? Path { get; set; }
        public string? QueryString { get; set; }
        public string? IP { get; set; }

        public int StatusCode { get; set; }
        public int STT { get; set; }


        public string? RequestBodyShort { get; set; }
        public string? ResponseBodyShort { get; set; }

        public long? DurationMs { get; set; }
        public string? ExceptionMessage { get; set; }
    }

}
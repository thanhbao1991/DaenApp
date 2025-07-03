namespace TraSuaApp.Shared.Dtos
{
    public class LoginResponse
    {
        public bool ThanhCong { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string TenHienThi { get; set; } = string.Empty;
        public string VaiTro { get; set; } = string.Empty;
    }
}
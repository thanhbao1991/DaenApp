namespace TraSuaApp.Shared.Dtos;

public class KhachHangPhoneDto
{
    public Guid IdKhachHang { get; set; }
    public Guid Id { get; set; }
    public string SoDienThoai { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
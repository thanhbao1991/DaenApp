namespace TraSuaApp.Shared.Dtos;

public class KhachHangAddressDto
{
    public Guid IdKhachHang { get; set; }
    public Guid Id { get; set; }
    public string DiaChi { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
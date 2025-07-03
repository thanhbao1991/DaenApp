namespace TraSuaApp.Domain.Entities;

public class CustomerPhoneNumber
{
    public Guid Id { get; set; }
    public Guid IdKhachHang { get; set; }
    public string SoDienThoai { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public KhachHang KhachHang { get; set; }
}
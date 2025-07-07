namespace TraSuaApp.Domain.Entities;

public class KhachHangAddress
{
    public Guid Id { get; set; }
    public Guid IdKhachHang { get; set; }
    public string DiaChi { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public KhachHang KhachHang { get; set; }
}